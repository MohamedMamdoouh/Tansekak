using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class StudentTrackInferrer
{
    public static AcademicTrack? TryInferFromCaseDesc(string? caseDesc)
    {
        if (string.IsNullOrWhiteSpace(caseDesc))
            return null;

        var text = caseDesc.Trim();

        if (text.Contains("علمي علوم", StringComparison.Ordinal))
            return AcademicTrack.Science;

        if (text.Contains("علمي رياضة", StringComparison.Ordinal))
            return AcademicTrack.Mathematics;

        if (text.Contains("أدبي", StringComparison.Ordinal))
            return AcademicTrack.Literature;

        return null;
    }

    public static string? ToDisplayName(AcademicTrack? track) =>
        track is null ? null : TrackHelper.ToDisplayName(track.Value);
}
