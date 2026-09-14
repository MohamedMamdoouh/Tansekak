using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Seeding;

public static class FacultySeedMapper
{
    public static Faculty MapFaculty(SeedFaculty seed) => new()
    {
        Id = seed.Id,
        NameAr = seed.NameAr,
        AllowedTracks = MapAllowedTracks(seed.AllowedTracks)
    };

    public static List<AcademicTrack> MapAllowedTracks(IEnumerable<string>? allowedTracks) =>
        (allowedTracks ?? [])
            .Select(ParseTrack)
            .Select(TrackHelper.Canonical)
            .Distinct()
            .ToList();

    private static AcademicTrack ParseTrack(string track)
    {
        if (TrackHelper.TryParse(track, out var parsed))
            return parsed;

        throw new InvalidOperationException($"Invalid track value in seed data: \"{track}\".");
    }
}
