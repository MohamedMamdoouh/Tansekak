using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class StudentTrackInferrer
{
    public static AcademicTrack? TryInferFromCaseDesc(string? caseDesc)
    {
        if (string.IsNullOrWhiteSpace(caseDesc))
            return null;

        var text = NormalizeForMatch(caseDesc);

        if (ContainsLiteratureTrack(text))
            return AcademicTrack.Literature;

        if (ContainsScientificTrack(text))
            return AcademicTrack.Science;

        return null;
    }

    public static string? ToDisplayName(AcademicTrack? track) =>
        track is null ? null : TrackHelper.ToDisplayName(TrackHelper.Canonical(track.Value));

    internal static string NormalizeForMatch(string text) =>
        text.Trim()
            .Replace('ى', 'ي')
            .Replace('أ', 'ا')
            .Replace('إ', 'ا')
            .Replace('آ', 'ا');

    internal static bool ContainsScientificTrack(string normalizedText) =>
        normalizedText.Contains("علمي علوم", StringComparison.Ordinal) ||
        normalizedText.Contains("علمي رياضة", StringComparison.Ordinal) ||
        normalizedText.Contains("علمي رياضه", StringComparison.Ordinal) ||
        normalizedText.Contains("الشعبة العلمية", StringComparison.Ordinal) ||
        normalizedText.Contains("علمي", StringComparison.Ordinal);

    internal static bool ContainsScienceTrack(string normalizedText) =>
        ContainsScientificTrack(normalizedText);

    internal static bool ContainsMathematicsTrack(string normalizedText) =>
        normalizedText.Contains("علمي رياضة", StringComparison.Ordinal) ||
        normalizedText.Contains("علمي رياضه", StringComparison.Ordinal);

    internal static bool ContainsLiteratureTrack(string normalizedText) =>
        normalizedText.Contains("ادبي", StringComparison.Ordinal) ||
        normalizedText.Contains("الادبي", StringComparison.Ordinal);
}
