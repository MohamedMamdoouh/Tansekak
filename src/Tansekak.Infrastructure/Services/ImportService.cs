using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class ImportService(AppDbContext db, ILogger<ImportService> logger) : IImportService
{
    public async Task<ImportResultDto> ImportAsync(
        int yearId,
        string selectedTrackName,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!TrackHelper.TryParse(selectedTrackName, out var selectedTrack))
            throw new ArgumentException("Invalid track.");

        var year = await db.AdmissionYears.FindAsync([yearId], cancellationToken)
            ?? throw new ArgumentException("Admission year not found.");

        var (parsedRows, parseErrors) = CutoffMarkdownParser.Parse(fileStream);
        if (parseErrors.Count > 0)
        {
            logger.LogWarning("Markdown parse failed for year {YearId} with {Count} errors.", yearId, parseErrors.Count);
            return new ImportResultDto(false, "Validation failed.", Errors: parseErrors);
        }

        var catalog = await LoadCatalogAsync(cancellationToken);
        var overrides = CutoffOverrideLoader.Load();
        var resolver = new CutoffNameResolver(catalog, overrides);
        var (resolvedRows, unresolvedErrors) = resolver.Resolve(parsedRows);

        var errors = new List<ImportValidationErrorDto>(unresolvedErrors);
        errors.AddRange(await ValidateResolvedRowsAsync(resolvedRows, year, selectedTrack, cancellationToken));

        if (errors.Count > 0)
        {
            logger.LogWarning("Import validation failed for year {YearId} with {Count} errors.", yearId, errors.Count);
            return new ImportResultDto(false, "Validation failed.", Errors: errors);
        }

        if (resolvedRows.Count == 0)
            return new ImportResultDto(false, "Validation failed.", Errors:
            [
                new ImportValidationErrorDto(0, "File", "EMPTY", "File contains no data rows.")
            ]);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.AdmissionCutoffs
                .Where(c => c.AdmissionYearId == yearId && c.Track == selectedTrack)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var row in resolvedRows)
            {
                db.AdmissionCutoffs.Add(new AdmissionCutoff
                {
                    AdmissionYearId = yearId,
                    UniversityFacultyId = row.UniversityFacultyId,
                    Track = selectedTrack,
                    CutoffScore = row.CutoffScore
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var trackLabel = TrackHelper.ToArabicName(selectedTrack);
            logger.LogInformation(
                "Imported {Count} cutoffs for year {YearId}, track {Track}.",
                resolvedRows.Count,
                yearId,
                selectedTrack);

            return new ImportResultDto(
                true,
                $"تم استيراد {resolvedRows.Count} حد قبول (استبدال شعبة {trackLabel}).",
                resolvedRows.Count);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<List<CutoffCatalogEntry>> LoadCatalogAsync(CancellationToken cancellationToken)
    {
        return await db.UniversityFaculties.AsNoTracking()
            .Select(uf => new CutoffCatalogEntry(
                uf.Id,
                uf.University.NameAr,
                uf.Faculty.NameAr))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ImportValidationErrorDto>> ValidateResolvedRowsAsync(
        List<ResolvedCutoffRow> rows,
        AdmissionYear year,
        AcademicTrack selectedTrack,
        CancellationToken cancellationToken)
    {
        var errors = new List<ImportValidationErrorDto>();
        var seen = new HashSet<int>();

        var ufIds = rows.Select(x => x.UniversityFacultyId).Distinct().ToList();
        var facultiesByUfId = await db.UniversityFaculties.AsNoTracking()
            .Include(x => x.Faculty)
            .Where(x => ufIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Faculty, cancellationToken);

        foreach (var row in rows)
        {
            if (row.CutoffScore <= 0)
                errors.Add(Err(row.LineNumber, "الحد الأدنى", "INVALID", "Score must be greater than zero."));
            else if (row.CutoffScore > year.MaximumScore)
                errors.Add(Err(row.LineNumber, "الحد الأدنى", "INVALID", $"Score must not exceed {year.MaximumScore}."));

            if (!seen.Add(row.UniversityFacultyId))
                errors.Add(Err(row.LineNumber, "الكلية", "DUPLICATE", "Duplicate row in file."));

            if (facultiesByUfId.TryGetValue(row.UniversityFacultyId, out var faculty)
                && !FacultyTrackValidator.IsTrackAllowed(faculty, selectedTrack))
            {
                errors.Add(Err(
                    row.LineNumber,
                    "الكلية",
                    "TRACK_NOT_ALLOWED",
                    FacultyTrackValidator.BuildRejectionMessage(faculty, selectedTrack)));
            }
        }

        return errors;
    }

    private static ImportValidationErrorDto Err(int row, string col, string code, string msg) =>
        new(row, col, code, msg);
}
