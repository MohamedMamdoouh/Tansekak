using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class StudentResultImportService(
    AppDbContext db,
    EntityIdAllocator idAllocator,
    ILogger<StudentResultImportService> logger) : IStudentResultImportService
{
    private const int BatchSize = 1000;

    private static readonly string ValidationFailedMessage =
        ArabicErrorCatalog.GetMessage(ApiErrorCodes.ValidationFailed);

    public async Task<ImportResultDto> ImportAsync(
        int yearId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return Fail(ApiErrorCodes.OnlyXlsxFiles);

        _ = await db.AdmissionYears.FindAsync([yearId], cancellationToken)
            ?? throw new NotFoundException(ApiErrorCodes.AdmissionYearNotFound);

        var (seekableStream, tempStream) = await EnsureSeekableStreamAsync(fileStream, cancellationToken);
        await using (tempStream)
        {
            return await ImportValidatedStreamAsync(yearId, seekableStream, cancellationToken);
        }
    }

    private async Task<ImportResultDto> ImportValidatedStreamAsync(
        int yearId,
        Stream seekableStream,
        CancellationToken cancellationToken)
    {
        var validation = StudentResultExcelParser.Validate(seekableStream);
        if (validation.Errors.Count > 0)
        {
            logger.LogWarning(
                "Excel parse failed for year {YearId} with {Count} errors.",
                yearId,
                validation.Errors.Count);
            return new ImportResultDto(false, ValidationFailedMessage, Errors: validation.Errors.ToList());
        }

        seekableStream.Position = 0;

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            if (db.Database.IsRelational())
            {
                await db.StudentResults
                    .Where(x => x.AdmissionYearId == yearId)
                    .ExecuteDeleteAsync(cancellationToken);
            }
            else
            {
                var existing = await db.StudentResults
                    .Where(x => x.AdmissionYearId == yearId)
                    .ToListAsync(cancellationToken);
                db.StudentResults.RemoveRange(existing);
                await db.SaveChangesAsync(cancellationToken);
            }

            db.ChangeTracker.Clear();

            var (startId, _) = await idAllocator.AllocateRangeAsync(
                EntityIdAllocator.StudentResults,
                validation.ValidRowCount,
                cancellationToken);

            var nextId = startId;
            var importedCount = 0;
            var batch = new List<ParsedStudentResultRow>(BatchSize);

            foreach (var row in StudentResultExcelParser.EnumerateRows(seekableStream))
            {
                batch.Add(row);
                if (batch.Count < BatchSize)
                    continue;

                nextId = await InsertBatchAsync(yearId, batch, nextId, cancellationToken);
                importedCount += batch.Count;
                batch.Clear();
            }

            if (batch.Count > 0)
            {
                nextId = await InsertBatchAsync(yearId, batch, nextId, cancellationToken);
                importedCount += batch.Count;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (transaction is not null)
                await transaction.CommitAsync(CancellationToken.None);

            logger.LogInformation("Imported {Count} student results for year {YearId}.", importedCount, yearId);
            return new ImportResultDto(
                true,
                $"تم استيراد {importedCount} نتيجة طالب (استبدال كامل للسنة).",
                importedCount);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<int> InsertBatchAsync(
        int yearId,
        IReadOnlyList<ParsedStudentResultRow> batch,
        int startId,
        CancellationToken cancellationToken)
    {
        var nextId = startId;

        foreach (var row in batch)
        {
            db.StudentResults.Add(new StudentResult
            {
                Id = nextId++,
                AdmissionYearId = yearId,
                SeatingNo = row.SeatingNo,
                ArabicName = row.ArabicName,
                TotalDegree = row.TotalDegree,
                StudentCaseDesc = row.StudentCaseDesc,
                Track = StudentTrackRankCalculator.ResolveTrack(new StudentResult
                {
                    SeatingNo = row.SeatingNo,
                    StudentCaseDesc = row.StudentCaseDesc,
                })
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return nextId;
    }

    private static async Task<(Stream Stream, FileStream? TempStream)> EnsureSeekableStreamAsync(
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        if (fileStream.CanSeek)
            return (fileStream, null);

        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"tansekak-import-{Guid.NewGuid():N}.xlsx");

        await using (var tempWrite = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         81920,
                         FileOptions.Asynchronous))
        {
            await fileStream.CopyToAsync(tempWrite, cancellationToken);
        }

        var tempStream = new FileStream(
            tempPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.DeleteOnClose);
        return (tempStream, tempStream);
    }

    private static ImportResultDto Fail(string errorCode) =>
        new(false, ArabicErrorCatalog.GetMessage(errorCode));
}
