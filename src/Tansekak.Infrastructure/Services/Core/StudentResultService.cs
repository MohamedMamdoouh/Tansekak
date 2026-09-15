using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Queries;

namespace Tansekak.Infrastructure.Services;

public class StudentResultService(AppDbContext db, CurrentAdmissionYearProvider yearProvider) : IStudentResultService
{
    public async Task<StudentResultDto?> GetBySeatingNoAsync(string seatingNo, CancellationToken cancellationToken = default)
    {
        var normalized = seatingNo.Trim();
        if (string.IsNullOrEmpty(normalized))
            return null;

        var currentYear = await yearProvider.GetCurrentAsync(cancellationToken);

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
            var peers = StudentResultTrackQuery.FilterByTrack(
                db.StudentResults.AsNoTracking()
                    .Where(x => x.AdmissionYearId == currentYear.Id),
                resolvedTrack.Value);

            trackTotalStudents = await peers.CountAsync(cancellationToken);

            if (trackTotalStudents > 0)
            {
                var entitySeatingNo = entity.SeatingNo;
                var higherRankCount = await peers.CountAsync(
                    x => x.TotalDegree > entity.TotalDegree
                        || (x.TotalDegree == entity.TotalDegree
                            && x.SeatingNo.CompareTo(entitySeatingNo) < 0),
                    cancellationToken);

                trackRank = higherRankCount + 1;
            }
        }

        return new StudentResultDto(
            entity.SeatingNo,
            entity.ArabicName,
            entity.TotalDegree,
            entity.StudentCaseDesc,
            currentYear.Year,
            currentYear.MaximumScore,
            track,
            trackRank,
            trackTotalStudents);
    }
}
