using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Seeding;

namespace Tansekak.Infrastructure.Services;

public class CutoffResyncService(
    AppDbContext db,
    IHostEnvironment env,
    ILogger<CutoffResyncService> logger) : ICutoffResyncService
{
    public async Task<CutoffResyncResultDto> ResyncYearFromSeedAsync(int yearId, CancellationToken cancellationToken = default)
    {
        var year = await db.AdmissionYears.FindAsync([yearId], cancellationToken)
            ?? throw new ArgumentException("Admission year not found.");

        var dataPath = SeedDataPathResolver.Resolve(env)
            ?? throw new DirectoryNotFoundException("Seed data folder not found.");

        await SyncFacultyAllowedTracksAsync(dataPath, cancellationToken);

        var validUfIds = await db.UniversityFaculties.AsNoTracking()
            .Select(x => x.Id)
            .ToHashSetAsync(cancellationToken);

        var seedCutoffs = (await CutoffSeedLoader.LoadCutoffsAsync(dataPath, yearId, cancellationToken))
            .Where(x => validUfIds.Contains(x.UniversityFacultyId))
            .ToList();

        if (seedCutoffs.Count == 0)
            throw new InvalidOperationException($"No seed cutoffs found for admission year {year.Year}.");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var deleted = await db.AdmissionCutoffs
            .Where(c => c.AdmissionYearId == yearId)
            .ExecuteDeleteAsync(cancellationToken);

        db.AdmissionCutoffs.AddRange(seedCutoffs);
        await db.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        logger.LogWarning(
            "Resynced admission cutoffs for year {YearId} ({Year}): deleted {Deleted}, inserted {Inserted}.",
            yearId,
            year.Year,
            deleted,
            seedCutoffs.Count);

        return new CutoffResyncResultDto(
            true,
            $"تمت إعادة مزامنة {seedCutoffs.Count} حد قبول لسنة {year.Year}.",
            deleted,
            seedCutoffs.Count);
    }

    private async Task SyncFacultyAllowedTracksAsync(string dataPath, CancellationToken cancellationToken)
    {
        var seedFaculties = await ReadFacultiesAsync(dataPath, cancellationToken);
        if (seedFaculties.Count == 0)
            return;

        var faculties = await db.Faculties.ToListAsync(cancellationToken);
        foreach (var faculty in faculties)
        {
            var seed = seedFaculties.FirstOrDefault(x => x.Id == faculty.Id);
            if (seed?.AllowedTracks is null || seed.AllowedTracks.Count == 0)
                continue;

            faculty.AllowedTracks = seed.AllowedTracks
                .Select(CutoffSeedLoader.TrackParse)
                .Distinct()
                .ToList();
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<SeedFaculty>> ReadFacultiesAsync(string dataPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(Path.Combine(dataPath, "Faculties.json"));
        var result = await System.Text.Json.JsonSerializer.DeserializeAsync<List<SeedFaculty>>(
            stream,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);
        return result ?? [];
    }

    private record SeedFaculty(int Id, string NameAr, List<string>? AllowedTracks);
}
