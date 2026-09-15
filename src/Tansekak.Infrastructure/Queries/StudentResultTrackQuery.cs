using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Queries;

internal static class StudentResultTrackQuery
{
    /// <summary>
    /// Filters peers for track ranking using persisted <see cref="StudentResult.Track"/>.
    /// </summary>
    public static IQueryable<StudentResult> FilterByTrack(
        IQueryable<StudentResult> query,
        AcademicTrack track) =>
        query.Where(x => x.Track == track);
}
