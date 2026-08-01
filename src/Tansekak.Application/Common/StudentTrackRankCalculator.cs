using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class StudentTrackRankCalculator
{
    public static AcademicTrack? ResolveTrack(StudentResult entity) =>
        entity.Track ?? StudentTrackInferrer.TryInferFromCaseDesc(entity.StudentCaseDesc);

    public static bool RanksHigher(
        decimal score,
        string seatingNo,
        decimal peerScore,
        string peerSeatingNo)
    {
        if (peerScore != score)
            return peerScore > score;

        return string.Compare(peerSeatingNo, seatingNo, StringComparison.Ordinal) < 0;
    }

    public static IQueryable<StudentResult> FilterByTrack(
        IQueryable<StudentResult> query,
        AcademicTrack track) =>
        track switch
        {
            AcademicTrack.Science => query.Where(x =>
                x.Track == AcademicTrack.Science ||
                (x.Track == null && x.StudentCaseDesc.Contains("علمي علوم"))),
            AcademicTrack.Mathematics => query.Where(x =>
                x.Track == AcademicTrack.Mathematics ||
                (x.Track == null && x.StudentCaseDesc.Contains("علمي رياضة"))),
            AcademicTrack.Literature => query.Where(x =>
                x.Track == AcademicTrack.Literature ||
                (x.Track == null && x.StudentCaseDesc.Contains("أدبي"))),
            _ => query.Where(_ => false)
        };
}
