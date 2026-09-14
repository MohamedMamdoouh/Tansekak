using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class TrackHelper
{
    private static readonly Dictionary<string, AcademicTrack> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Science"] = AcademicTrack.Science,
        ["Mathematics"] = AcademicTrack.Science,
        ["Literature"] = AcademicTrack.Literature,
        ["علمي"] = AcademicTrack.Science,
        ["علمي علوم"] = AcademicTrack.Science,
        ["علمي رياضة"] = AcademicTrack.Science,
        ["علمي رياضه"] = AcademicTrack.Science,
        ["الشعبة العلمية"] = AcademicTrack.Science,
        ["شعبة علمية"] = AcademicTrack.Science,
        ["أدبي"] = AcademicTrack.Literature,
        ["ادبي"] = AcademicTrack.Literature,
        ["الادبي"] = AcademicTrack.Literature,
        ["الشعبة الأدبية"] = AcademicTrack.Literature,
        ["الشعبة الادبية"] = AcademicTrack.Literature,
        ["شعبة أدبية"] = AcademicTrack.Literature,
        ["شعبة ادبية"] = AcademicTrack.Literature
    };

    public static AcademicTrack Canonical(AcademicTrack track) =>
        track == AcademicTrack.Mathematics ? AcademicTrack.Science : track;

    public static IReadOnlyList<AcademicTrack> TracksInBucket(AcademicTrack track)
    {
        var canonical = Canonical(track);
        return canonical == AcademicTrack.Science
            ? [AcademicTrack.Science, AcademicTrack.Mathematics]
            : [canonical];
    }

    public static bool IsInBucket(AcademicTrack stored, AcademicTrack requested) =>
        Canonical(stored) == Canonical(requested);

    public static bool AllowsTrack(IEnumerable<AcademicTrack> allowedTracks, AcademicTrack track)
    {
        var canonical = Canonical(track);
        return allowedTracks.Any(t => Canonical(t) == canonical);
    }

    public static bool TryParse(string? value, out AcademicTrack track)
    {
        var normalized = Normalize(value);
        if (Map.TryGetValue(normalized, out track))
        {
            track = Canonical(track);
            return true;
        }

        if (StudentTrackInferrer.TryInferFromCaseDesc(value) is AcademicTrack inferred)
        {
            track = Canonical(inferred);
            return true;
        }

        track = default;
        return false;
    }

    public static string ToDisplayName(AcademicTrack track) => Canonical(track) switch
    {
        AcademicTrack.Science => "Science",
        AcademicTrack.Literature => "Literature",
        _ => track.ToString()
    };

    public static string ToArabicName(AcademicTrack track) => Canonical(track) switch
    {
        AcademicTrack.Science => "الشعبة العلمية",
        AcademicTrack.Literature => "الشعبة الأدبية",
        _ => track.ToString()
    };

    public static string ToArabicName(string track) =>
        TryParse(track, out var t) ? ToArabicName(t) : track;

    public static IReadOnlyList<string> AllTracks { get; } =
        ["Science", "Literature"];

    private static string Normalize(string? value) =>
        StudentTrackInferrer.NormalizeForMatch(value ?? string.Empty);
}
