using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class StudentTrackRankCalculator
{
    public static AcademicTrack? ResolveTrack(StudentResult entity) =>
        entity.Track
            ?? StudentTrackInferrer.TryInferFromCaseDesc(entity.StudentCaseDesc)
            ?? SeatingNumberTrackInferrer.TryInferFromSeatingNo(entity.SeatingNo);

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
}
