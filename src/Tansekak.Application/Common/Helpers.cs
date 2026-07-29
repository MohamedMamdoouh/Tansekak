using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class TrackHelper
{
    private static readonly Dictionary<string, AcademicTrack> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Science"] = AcademicTrack.Science,
        ["Mathematics"] = AcademicTrack.Mathematics,
        ["Literature"] = AcademicTrack.Literature,
        ["علمي علوم"] = AcademicTrack.Science,
        ["علمي رياضة"] = AcademicTrack.Mathematics,
        ["أدبي"] = AcademicTrack.Literature
    };

    public static bool TryParse(string? value, out AcademicTrack track) =>
        Map.TryGetValue(value ?? string.Empty, out track);

    public static string ToDisplayName(AcademicTrack track) => track switch
    {
        AcademicTrack.Science => "Science",
        AcademicTrack.Mathematics => "Mathematics",
        AcademicTrack.Literature => "Literature",
        _ => track.ToString()
    };

    public static string ToArabicName(AcademicTrack track) => track switch
    {
        AcademicTrack.Science => "علمي علوم",
        AcademicTrack.Mathematics => "علمي رياضة",
        AcademicTrack.Literature => "أدبي",
        _ => track.ToString()
    };

    public static string ToArabicName(string track) =>
        TryParse(track, out var t) ? ToArabicName(t) : track;

    public static IReadOnlyList<string> AllTracks { get; } =
        [ "Science", "Mathematics", "Literature" ];
}

public static class UniversityTypeHelper
{
    public static bool TryParse(string? value, out UniversityType type)
    {
        type = UniversityType.Public;
        return Enum.TryParse(value, true, out type);
    }
}
