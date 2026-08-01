using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class ConfigService(AppDbContext db, IOptions<TansekakOptions> options) : IConfigService
{
    public async Task<ConfigDto> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var currentYear = await db.AdmissionYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsCurrent, cancellationToken)
            ?? throw new InvalidOperationException("No current admission year configured.");

        return new ConfigDto(
            options.Value.AppName,
            currentYear.Year,
            currentYear.MaximumScore,
            TrackHelper.AllTracks);
    }
}

public class AdmissionPredictionService(AppDbContext db) : IAdmissionPredictionService
{
    public async Task<PredictResponseDto> PredictAsync(PredictRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TrackHelper.TryParse(request.Track, out var track))
            throw new ArgumentException("Invalid track.");

        var currentYear = await db.AdmissionYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsCurrent, cancellationToken)
            ?? throw new InvalidOperationException("No current admission year configured.");

        if (request.Score > currentYear.MaximumScore)
            throw new ArgumentException($"Score must not exceed {currentYear.MaximumScore}.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var cutoffs = await db.AdmissionCutoffs.AsNoTracking()
            .Include(c => c.UniversityFaculty)
            .ThenInclude(uf => uf.University)
            .Include(c => c.UniversityFaculty)
            .ThenInclude(uf => uf.Faculty)
            .Where(c => c.AdmissionYearId == currentYear.Id && c.Track == track && request.Score >= c.CutoffScore)
            .ToListAsync(cancellationToken);

        var eligibleCutoffs = cutoffs
            .Where(c => FacultyTrackValidator.IsTrackAllowed(c.UniversityFaculty.Faculty, track))
            .Select(c => new
            {
                c.CutoffScore,
                UniversityName = c.UniversityFaculty.University.NameAr,
                FacultyName = c.UniversityFaculty.Faculty.NameAr
            })
            .ToList();

        var allResults = eligibleCutoffs
            .Select(c => new
            {
                c.UniversityName,
                c.FacultyName,
                SortKey = Math.Abs(request.Score - c.CutoffScore)
            })
            .OrderBy(x => x.SortKey)
            .Select(c => new AdmissionResultDto(
                new NamedEntityDto(c.UniversityName),
                new NamedEntityDto(c.FacultyName)))
            .ToList();

        var totalCount = allResults.Count;
        var skip = (page - 1) * pageSize;
        var pageItems = allResults.Skip(skip).Take(pageSize).ToList();
        var hasMore = skip + pageItems.Count < totalCount;

        return new PredictResponseDto(pageItems, hasMore, totalCount);
    }
}

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var currentYear = await db.AdmissionYears.AsNoTracking()
            .Where(x => x.IsCurrent).Select(x => (int?)x.Year).FirstOrDefaultAsync(cancellationToken);

        return new DashboardDto(
            await db.Governorates.CountAsync(cancellationToken),
            await db.Universities.CountAsync(cancellationToken),
            await db.Faculties.CountAsync(cancellationToken),
            await db.UniversityFaculties.CountAsync(cancellationToken),
            await db.AdmissionCutoffs.CountAsync(cancellationToken),
            await db.StudentResults.CountAsync(cancellationToken),
            currentYear);
    }
}

public class StudentResultService(AppDbContext db) : IStudentResultService
{
    public async Task<StudentResultDto?> GetBySeatingNoAsync(string seatingNo, CancellationToken cancellationToken = default)
    {
        var normalized = seatingNo.Trim();
        if (string.IsNullOrEmpty(normalized))
            return null;

        var currentYear = await db.AdmissionYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsCurrent, cancellationToken)
            ?? throw new InvalidOperationException("No current admission year configured.");

        var entity = await db.StudentResults.AsNoTracking()
            .Where(x => x.AdmissionYearId == currentYear.Id && x.SeatingNo == normalized)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return null;

        var resolvedTrack = StudentTrackRankCalculator.ResolveTrack(entity);
        var track = StudentTrackInferrer.ToDisplayName(resolvedTrack);

        int? trackRank = null;
        int? trackTotalStudents = null;

        if (resolvedTrack.HasValue)
        {
            var peers = StudentTrackRankCalculator.FilterByTrack(
                db.StudentResults.AsNoTracking()
                    .Where(x => x.AdmissionYearId == currentYear.Id),
                resolvedTrack.Value);

            trackTotalStudents = await peers.CountAsync(cancellationToken);

            if (trackTotalStudents > 0)
            {
                var higherCount = await peers.CountAsync(x =>
                    x.TotalDegree > entity.TotalDegree ||
                    (x.TotalDegree == entity.TotalDegree &&
                     string.Compare(x.SeatingNo, entity.SeatingNo, StringComparison.Ordinal) < 0),
                    cancellationToken);

                trackRank = higherCount + 1;
            }
        }

        return new StudentResultDto(
            entity.SeatingNo,
            entity.ArabicName,
            entity.TotalDegree,
            entity.StudentCaseDesc,
            currentYear.Year,
            track,
            trackRank,
            trackTotalStudents);
    }
}
