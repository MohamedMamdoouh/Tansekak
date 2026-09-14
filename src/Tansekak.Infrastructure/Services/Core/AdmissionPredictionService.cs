using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class AdmissionPredictionService(AppDbContext db, CurrentAdmissionYearProvider yearProvider) : IAdmissionPredictionService
{
    public async Task<PredictResponseDto> PredictAsync(PredictRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TrackHelper.TryParse(request.Track, out var track))
            throw new ValidationException(ApiErrorCodes.InvalidTrack);

        track = TrackHelper.Canonical(track);
        var bucketTracks = TrackHelper.TracksInBucket(track);

        var currentYear = await yearProvider.GetCurrentAsync(cancellationToken);

        if (request.Score > currentYear.MaximumScore)
            throw new ValidationException(
                ApiErrorCodes.ScoreExceedsMax,
                ArabicErrorCatalog.GetMessage(ApiErrorCodes.ScoreExceedsMax, currentYear.MaximumScore));

        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var skip = (page - 1) * pageSize;

        var matches = await db.AdmissionCutoffs.AsNoTracking()
            .Where(c => c.AdmissionYearId == currentYear.Id
                && bucketTracks.Contains(c.Track)
                && request.Score >= c.CutoffScore)
            .Select(c => new
            {
                c.Id,
                c.UniversityFacultyId,
                c.Track,
                c.CutoffScore,
                UniversityName = c.UniversityFaculty.University.NameAr,
                FacultyName = c.UniversityFaculty.Faculty.NameAr,
                AllowedTracks = c.UniversityFaculty.Faculty.AllowedTracks
            })
            .ToListAsync(cancellationToken);

        var deduped = matches
            .Where(x => TrackHelper.AllowsTrack(x.AllowedTracks, track))
            .GroupBy(x => x.UniversityFacultyId)
            .Select(g => g
                .OrderBy(x => x.Track == AcademicTrack.Science ? 0 : 1)
                .ThenBy(x => x.Id)
                .First())
            .Select(x => new
            {
                SortKey = Math.Abs(request.Score - x.CutoffScore),
                x.UniversityName,
                x.FacultyName
            })
            .OrderBy(x => x.SortKey)
            .ToList();

        var totalCount = deduped.Count;
        var pageItems = deduped
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new AdmissionResultDto(
                new NamedEntityDto(x.UniversityName),
                new NamedEntityDto(x.FacultyName)))
            .ToList();

        var hasMore = skip + pageItems.Count < totalCount;

        return new PredictResponseDto(pageItems, hasMore, totalCount);
    }
}
