using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Seeding;

public sealed class FacultyAllowedTracksRepairService(
    AppDbContext db,
    ISeedDataSource seedData,
    ILogger<FacultyAllowedTracksRepairService> logger)
{
    public async Task<int> RepairAsync(CancellationToken cancellationToken = default)
    {
        if (!await db.Faculties.AnyAsync(cancellationToken))
        {
            logger.LogDebug("Skipping faculty allowed-tracks repair because no faculties exist yet.");
            return 0;
        }

        var seedFaculties = await seedData.ReadListAsync<SeedFaculty>(SeedFileNames.Faculties, cancellationToken);
        if (seedFaculties.Count == 0)
            return 0;

        var expectedById = seedFaculties.ToDictionary(
            faculty => faculty.Id,
            faculty => FacultySeedMapper.MapAllowedTracks(faculty.AllowedTracks));

        var faculties = await db.Faculties.ToListAsync(cancellationToken);
        var repaired = 0;

        foreach (var faculty in faculties)
        {
            if (!expectedById.TryGetValue(faculty.Id, out var expectedTracks))
                continue;

            if (TracksMatch(faculty.AllowedTracks, expectedTracks))
                continue;

            faculty.AllowedTracks = expectedTracks;
            repaired++;
            logger.LogInformation(
                "Repaired allowed tracks for faculty {FacultyId} ({FacultyName}).",
                faculty.Id,
                faculty.NameAr);
        }

        if (repaired > 0)
            await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Faculty allowed-tracks repair completed. Updated {Count} faculties.", repaired);
        return repaired;
    }

    private static bool TracksMatch(IReadOnlyList<AcademicTrack> current, IReadOnlyList<AcademicTrack> expected) =>
        current.OrderBy(track => track).SequenceEqual(expected.OrderBy(track => track));
}
