using System.Text.Json;
using System.Text.Json.Serialization;
using Tansekak.Application.Common;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Persistence;

public static class FacultyAllowedTracksJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public static string Serialize(List<AcademicTrack> tracks) =>
        JsonSerializer.Serialize(tracks.Select(TrackHelper.Canonical).Distinct(), Options);

    public static List<AcademicTrack> Deserialize(string json)
    {
        var tracks = JsonSerializer.Deserialize<List<AcademicTrack>>(json, Options) ?? [];
        return tracks.Select(TrackHelper.Canonical).Distinct().ToList();
    }
}
