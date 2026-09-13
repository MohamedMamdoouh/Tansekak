using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class StudentTrackMatchRules
{
    public static bool MatchesTrack(
        AcademicTrack? explicitTrack,
        string? caseDesc,
        string? seatingNo,
        AcademicTrack target)
    {
        if (explicitTrack == target)
            return true;

        if (explicitTrack is not null)
            return false;

        var fromCase = StudentTrackInferrer.TryInferFromCaseDesc(caseDesc);
        if (fromCase == target)
            return true;

        if (fromCase is not null)
            return false;

        return SeatingNumberTrackInferrer.TryInferFromSeatingNo(seatingNo) == target;
    }
}
