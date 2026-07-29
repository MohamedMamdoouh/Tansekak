using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Seeding;

public class FacultyAllowedTracksSynchronizer(
    AppDbContext db,
    IHostEnvironment env,
    ILogger<FacultyAllowedTracksSynchronizer> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        var dataPath = SeedDataPathResolver.Resolve(env);
        if (dataPath is null)
            return;

        var seedFaculties = await ReadFacultiesAsync(dataPath, cancellationToken);
        if (seedFaculties.Count == 0)
            return;

        var faculties = await db.Faculties.ToListAsync(cancellationToken);
        var updated = 0;

        foreach (var faculty in faculties)
        {
            var seed = seedFaculties.FirstOrDefault(x => x.Id == faculty.Id);
            if (seed?.AllowedTracks is null || seed.AllowedTracks.Count == 0)
                continue;

            var tracks = seed.AllowedTracks
                .Select(CutoffSeedLoader.TrackParse)
                .Distinct()
                .ToList();

            if (faculty.AllowedTracks.SequenceEqual(tracks))
                continue;

            faculty.AllowedTracks = tracks;
            updated++;
        }

        if (updated == 0)
            return;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Synced AllowedTracks for {Count} faculties from seed data.", updated);
    }

    private static async Task<List<SeedFaculty>> ReadFacultiesAsync(string dataPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(Path.Combine(dataPath, "Faculties.json"));
        var result = await JsonSerializer.DeserializeAsync<List<SeedFaculty>>(stream, JsonOptions, cancellationToken);
        return result ?? [];
    }

    private record SeedFaculty(int Id, string NameAr, List<string>? AllowedTracks);
}
