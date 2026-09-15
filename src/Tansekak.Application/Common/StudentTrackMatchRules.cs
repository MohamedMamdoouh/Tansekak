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
        if (explicitTrack is not null)
            return explicitTrack.Value == target;

        var fromCase = StudentTrackInferrer.TryInferFromCaseDesc(caseDesc);
        if (fromCase is not null)
            return fromCase.Value == target;

        var fromSeating = SeatingNumberTrackInferrer.TryInferFromSeatingNo(seatingNo);
        return fromSeating is not null && fromSeating.Value == target;
    }
}
