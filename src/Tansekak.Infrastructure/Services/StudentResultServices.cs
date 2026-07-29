using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class StudentResultImportService(AppDbContext db, ILogger<StudentResultImportService> logger) : IStudentResultImportService
{
    private const int MaxReportedErrors = 50;

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
            return new ImportResultDto(false, "Validation failed.", Errors: LimitErrors(parseErrors));
        }

        var errors = ValidateRows(parsedRows);
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

        // Railway's HTTP proxy times out at ~5 minutes; don't cancel the DB write when the client disconnects.
        var persistCancellation = CancellationToken.None;

        await using var transaction = await db.Database.BeginTransactionAsync(persistCancellation);
        try
        {
            var deleted = await db.StudentResults
                .Where(r => r.AdmissionYearId == yearId)
                .ExecuteDeleteAsync(persistCancellation);

            logger.LogInformation(
                "Deleted {Deleted} existing student results for year {YearId}; inserting {Count} rows.",
                deleted,
                yearId,
                parsedRows.Count);

            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(persistCancellation);

            await BulkInsertAsync(connection, yearId, parsedRows, persistCancellation);

            await transaction.CommitAsync(persistCancellation);

            logger.LogInformation("Imported {Count} student results for year {YearId}.", parsedRows.Count, yearId);
            return new ImportResultDto(
                true,
                $"تم استيراد {parsedRows.Count} نتيجة طالب (استبدال نتائج سنة {year.Year}).",
                parsedRows.Count);
        }
        catch
        {
            await transaction.RollbackAsync(persistCancellation);
            throw;
        }
    }

    private static async Task BulkInsertAsync(
        NpgsqlConnection connection,
        int yearId,
        IReadOnlyList<ParsedStudentResultRow> rows,
        CancellationToken cancellationToken)
    {
        const string copySql =
            """
            COPY "StudentResults" ("AdmissionYearId", "SeatingNo", "ArabicName", "TotalDegree", "StudentCaseDesc")
            FROM STDIN (FORMAT BINARY)
            """;

        await using var writer = await connection.BeginBinaryImportAsync(copySql, cancellationToken);
        foreach (var row in rows)
        {
            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(yearId, NpgsqlDbType.Integer, cancellationToken);
            await writer.WriteAsync(row.SeatingNo, NpgsqlDbType.Varchar, cancellationToken);
            await writer.WriteAsync(row.ArabicName, NpgsqlDbType.Varchar, cancellationToken);
            await writer.WriteAsync(row.TotalDegree, NpgsqlDbType.Numeric, cancellationToken);
            await writer.WriteAsync(row.StudentCaseDesc, NpgsqlDbType.Varchar, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }

    private static List<ImportValidationErrorDto> ValidateRows(List<ParsedStudentResultRow> rows)
    {
        var errors = new List<ImportValidationErrorDto>();
        var seen = new HashSet<string>();

        foreach (var row in rows)
        {
            if (errors.Count >= MaxReportedErrors) break;

            if (row.TotalDegree < 0)
                errors.Add(Err(row.RowNumber, "total_degree", "INVALID", "Total degree cannot be negative."));

            if (!seen.Add(row.SeatingNo))
                errors.Add(Err(row.RowNumber, "seating_no", "DUPLICATE", "Duplicate seating number in file."));
        }

        return errors;
    }

    private static List<ImportValidationErrorDto> LimitErrors(List<ImportValidationErrorDto> errors) =>
        errors.Count <= MaxReportedErrors ? errors : errors.Take(MaxReportedErrors).ToList();

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
