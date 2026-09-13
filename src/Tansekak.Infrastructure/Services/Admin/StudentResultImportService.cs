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

    public async Task<ImportResultDto> ImportAsync(
        int yearId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return Fail("Only .xlsx files are supported.");

        _ = await db.AdmissionYears.FindAsync([yearId], cancellationToken)
            ?? throw new NotFoundException("Admission year not found.");

        var (parsedRows, parseErrors) = StudentResultExcelParser.Parse(fileStream);
        if (parseErrors.Count > 0)
        {
            logger.LogWarning("Excel parse failed for year {YearId} with {Count} errors.", yearId, parseErrors.Count);
            return new ImportResultDto(false, "Validation failed.", Errors: parseErrors);
        }

        var validationErrors = ValidateRows(parsedRows);
        if (validationErrors.Count > 0)
        {
            logger.LogWarning("Import validation failed for year {YearId} with {Count} errors.", yearId, validationErrors.Count);
            return new ImportResultDto(false, "Validation failed.", Errors: validationErrors);
        }

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
            }

            var (startId, _) = await idAllocator.AllocateRangeAsync(
                EntityIdAllocator.StudentResults,
                parsedRows.Count,
                cancellationToken);

            for (var offset = 0; offset < parsedRows.Count; offset += BatchSize)
            {
                var batch = parsedRows.Skip(offset).Take(BatchSize).ToList();
                var nextId = startId + offset;

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
                        Track = StudentTrackInferrer.TryInferFromCaseDesc(row.StudentCaseDesc)
                    });
                }

                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Imported {Count} student results for year {YearId}.", parsedRows.Count, yearId);
            return new ImportResultDto(
                true,
                $"تم استيراد {parsedRows.Count} نتيجة طالب (استبدال كامل للسنة).",
                parsedRows.Count);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static List<ImportValidationErrorDto> ValidateRows(List<ParsedStudentResultRow> rows)
    {
        var errors = new List<ImportValidationErrorDto>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (!seen.Add(row.SeatingNo))
                errors.Add(Err(row.RowNumber, "seating_no", "DUPLICATE", "Duplicate seating number in file."));
        }

        return errors;
    }

    private static ImportResultDto Fail(string message) =>
        new(false, message);

    private static ImportValidationErrorDto Err(int row, string col, string code, string msg) =>
        new(row, col, code, msg);
}
