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
            throw new ValidationException(ApiErrorCodes.InvalidTrack);

        var currentYear = await yearProvider.GetCurrentAsync(cancellationToken);

        if (request.Score > currentYear.MaximumScore)
            throw new ValidationException(
                ApiErrorCodes.ScoreExceedsMax,
                ArabicErrorCatalog.GetMessage(ApiErrorCodes.ScoreExceedsMax, currentYear.MaximumScore));

        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var skip = (page - 1) * pageSize;
        var score = request.Score;

        var matches = await db.AdmissionCutoffs.AsNoTracking()
            .Where(c => c.AdmissionYearId == currentYear.Id
                && c.Track == track
                && score >= c.CutoffScore)
            .Select(c => new
            {
                SortKey = Math.Abs(score - c.CutoffScore),
                UniversityName = c.UniversityFaculty.University.NameAr,
                FacultyName = c.UniversityFaculty.Faculty.NameAr,
                AllowedTracks = c.UniversityFaculty.Faculty.AllowedTracks,
            })
            .ToListAsync(cancellationToken);

        var eligible = matches
            .Where(x => FacultyTrackValidator.IsTrackAllowed(
                new Tansekak.Domain.Entities.Faculty { AllowedTracks = x.AllowedTracks },
                track))
            .OrderBy(x => x.SortKey)
            .ToList();

        var totalCount = eligible.Count;
        var pageItems = eligible
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
