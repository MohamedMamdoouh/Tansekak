using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class StudentResultImportService(AppDbContext db, ILogger<StudentResultImportService> logger) : IStudentResultImportService
{
    public async Task<ImportResultDto> ImportAsync(
        int yearId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var year = await db.AdmissionYears.FindAsync([yearId], cancellationToken)
            ?? throw new ArgumentException("Admission year not found.");

        logger.LogInformation("Starting student results import for year {YearId}, file {FileName}.", yearId, fileName);

        var (parsedRows, parseErrors) = await Task.Run(
            () => StudentResultExcelParser.Parse(fileStream),
            cancellationToken);
        if (parseErrors.Count > 0)
        {
            logger.LogWarning("Excel parse failed for year {YearId} with {Count} errors.", yearId, parseErrors.Count);
            return new ImportResultDto(false, "Validation failed.", Errors: parseErrors);
        }

        var errors = ValidateRows(parsedRows, year);
        if (errors.Count > 0)
        {
            logger.LogWarning("Import validation failed for year {YearId} with {Count} errors.", yearId, errors.Count);
            return new ImportResultDto(false, "Validation failed.", Errors: errors);
        }

        if (parsedRows.Count == 0)
        {
            return new ImportResultDto(false, "Validation failed.", Errors:
            [
                new ImportValidationErrorDto(0, "File", "EMPTY", "File contains no data rows.")
            ]);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.StudentResults
                .Where(r => r.AdmissionYearId == yearId)
                .ExecuteDeleteAsync(cancellationToken);

            const int batchSize = 2000;
            for (var i = 0; i < parsedRows.Count; i += batchSize)
            {
                var entities = parsedRows.Skip(i).Take(batchSize).Select(row => new StudentResult
                {
                    AdmissionYearId = yearId,
                    SeatingNo = row.SeatingNo,
                    ArabicName = row.ArabicName,
                    TotalDegree = row.TotalDegree,
                    StudentCaseDesc = row.StudentCaseDesc
                }).ToList();

                db.StudentResults.AddRange(entities);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Imported {Count} student results for year {YearId}.", parsedRows.Count, yearId);
            return new ImportResultDto(
                true,
                $"تم استيراد {parsedRows.Count} نتيجة طالب (استبدال نتائج سنة {year.Year}).",
                parsedRows.Count);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static List<ImportValidationErrorDto> ValidateRows(List<ParsedStudentResultRow> rows, AdmissionYear year)
    {
        var errors = new List<ImportValidationErrorDto>();
        var seen = new HashSet<string>();

        foreach (var row in rows)
        {
            if (row.TotalDegree < 0)
                errors.Add(Err(row.RowNumber, "total_degree", "INVALID", "Total degree cannot be negative."));
            else if (row.TotalDegree > year.MaximumScore)
                errors.Add(Err(row.RowNumber, "total_degree", "INVALID", $"Total degree must not exceed {year.MaximumScore}."));

            if (!seen.Add(row.SeatingNo))
                errors.Add(Err(row.RowNumber, "seating_no", "DUPLICATE", "Duplicate seating number in file."));
        }

        return errors;
    }

    private static ImportValidationErrorDto Err(int row, string col, string code, string msg) =>
        new(row, col, code, msg);
}

public class StudentResultService(AppDbContext db) : IStudentResultService
{
    public async Task<StudentResultDto?> LookupBySeatingNoAsync(string seatingNo, CancellationToken cancellationToken = default)
    {
        var normalized = StudentResultExcelParser.NormalizeSeatingNo(seatingNo);
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        var currentYear = await db.AdmissionYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsCurrent, cancellationToken);
        if (currentYear is null) return null;

        var result = await db.StudentResults.AsNoTracking()
            .Where(r => r.AdmissionYearId == currentYear.Id && r.SeatingNo == normalized)
            .Select(r => new StudentResultDto(
                r.SeatingNo,
                r.ArabicName,
                r.TotalDegree,
                r.StudentCaseDesc,
                currentYear.Year))
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }
}
