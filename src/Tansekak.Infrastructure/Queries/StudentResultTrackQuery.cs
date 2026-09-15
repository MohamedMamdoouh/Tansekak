using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Queries;

internal static class StudentResultTrackQuery
{
    /// <summary>
    /// Filters peers for track ranking using persisted <see cref="StudentResult.Track"/> buckets.
    /// </summary>
    public static IQueryable<StudentResult> FilterByTrack(
        IQueryable<StudentResult> query,
        AcademicTrack track)
    {
        var canonical = TrackHelper.Canonical(track);
        return canonical switch
        {
            AcademicTrack.Science => query.Where(x =>
                x.Track == AcademicTrack.Science || x.Track == AcademicTrack.Mathematics),
            AcademicTrack.Literature => query.Where(x => x.Track == AcademicTrack.Literature),
            _ => query.Where(_ => false),
        };
    }
}
