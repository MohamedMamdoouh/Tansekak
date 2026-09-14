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
        var canonicalTarget = TrackHelper.Canonical(target);

        if (explicitTrack is not null)
            return TrackHelper.Canonical(explicitTrack.Value) == canonicalTarget;

        var fromCase = StudentTrackInferrer.TryInferFromCaseDesc(caseDesc);
        if (fromCase is not null)
            return TrackHelper.Canonical(fromCase.Value) == canonicalTarget;

        var fromSeating = SeatingNumberTrackInferrer.TryInferFromSeatingNo(seatingNo);
        return fromSeating is not null && TrackHelper.Canonical(fromSeating.Value) == canonicalTarget;
    }
}
