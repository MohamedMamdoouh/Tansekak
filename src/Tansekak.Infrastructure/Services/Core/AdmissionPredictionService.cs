using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class AdmissionPredictionService(AppDbContext db, CurrentAdmissionYearProvider yearProvider) : IAdmissionPredictionService
{
    public async Task<PredictResponseDto> PredictAsync(PredictRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TrackHelper.TryParse(request.Track, out var track))
            throw new ArgumentException("Invalid track.");

        var currentYear = await yearProvider.GetCurrentAsync(cancellationToken);

        if (request.Score > currentYear.MaximumScore)
            throw new ArgumentException($"Score must not exceed {currentYear.MaximumScore}.");

        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var skip = (page - 1) * pageSize;

        var baseQuery = db.AdmissionCutoffs.AsNoTracking()
            .Where(c => c.AdmissionYearId == currentYear.Id
                && c.Track == track
                && request.Score >= c.CutoffScore
                && c.UniversityFaculty.Faculty.AllowedTracks.Contains(track));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var pageItems = await baseQuery
            .Select(c => new
            {
                SortKey = Math.Abs(request.Score - c.CutoffScore),
                UniversityName = c.UniversityFaculty.University.NameAr,
                FacultyName = c.UniversityFaculty.Faculty.NameAr
            })
            .OrderBy(x => x.SortKey)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new AdmissionResultDto(
                new NamedEntityDto(x.UniversityName),
                new NamedEntityDto(x.FacultyName)))
            .ToListAsync(cancellationToken);

        var hasMore = skip + pageItems.Count < totalCount;

        return new PredictResponseDto(pageItems, hasMore, totalCount);
    }
}
