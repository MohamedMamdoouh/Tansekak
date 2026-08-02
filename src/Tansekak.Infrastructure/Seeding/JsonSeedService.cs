using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Seeding;

namespace Tansekak.Infrastructure.Seeding;

public class JsonSeedService(
    AppDbContext db,
    IHostEnvironment env,
    FacultyAllowedTracksSynchronizer allowedTracksSynchronizer,
    ILogger<JsonSeedService> logger) : IDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await allowedTracksSynchronizer.SyncAsync(cancellationToken);

        if (await db.AdmissionCutoffs.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded.");
            await SyncCurrentAdmissionYearFromSeedAsync(cancellationToken);
            return;
        }

        if (await db.Governorates.AnyAsync(cancellationToken))
        {
            logger.LogWarning("Partial seed detected. Clearing business data before reseeding.");
            await ClearBusinessDataAsync(cancellationToken);
        }

        var dataPath = SeedDataPathResolver.Resolve(env);
        if (dataPath is null)
        {
            throw new DirectoryNotFoundException(
                "Seed data folder not found. Expected 'SeededData' or 'Data' at the repository root.");
        }

        logger.LogInformation("Loading seed data from {DataPath}.", dataPath);
        var governorates = await ReadJsonAsync<SeedGovernorate>(Path.Combine(dataPath, "Governorates.json"), cancellationToken);
        var faculties = await ReadJsonAsync<SeedFaculty>(Path.Combine(dataPath, "Faculties.json"), cancellationToken);
        var universities = await ReadJsonAsync<SeedUniversity>(Path.Combine(dataPath, "Universities.json"), cancellationToken);
        var universityFaculties = await ReadJsonAsync<SeedUniversityFaculty>(Path.Combine(dataPath, "UniversityFaculties.json"), cancellationToken);
        var years = await ReadJsonAsync<SeedAdmissionYear>(Path.Combine(dataPath, "AdmissionYears.json"), cancellationToken);
        var cutoffs = await ReadJsonAsync<SeedCutoff>(CutoffSeedLoader.ResolveCutoffsFile(dataPath), cancellationToken);

        var governorateIds = governorates.Select(x => x.Id).ToHashSet();
        var facultyIds = faculties.Select(x => x.Id).ToHashSet();
        var universityIds = universities.Select(x => x.Id).ToHashSet();

        var validUniversityFaculties = universityFaculties
            .Where(x => universityIds.Contains(x.UniversityId) && facultyIds.Contains(x.FacultyId))
            .ToList();

        var skippedUf = universityFaculties.Count - validUniversityFaculties.Count;
        if (skippedUf > 0)
            logger.LogWarning("Skipped {Count} university-faculty rows with invalid references.", skippedUf);

        var validUfIds = validUniversityFaculties.Select(x => x.Id).ToHashSet();
        var validCutoffs = cutoffs.Where(x => validUfIds.Contains(x.UniversityFacultyId)).ToList();

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        db.Governorates.AddRange(governorates.Select(x => new Governorate { Id = x.Id, NameAr = x.NameAr }));
        db.Faculties.AddRange(faculties.Select(MapFaculty));
        db.Universities.AddRange(universities
            .Where(x => governorateIds.Contains(x.GovernorateId))
            .Select(x => new University
            {
                Id = x.Id,
                NameAr = x.NameAr,
                GovernorateId = x.GovernorateId,
                Type = Enum.TryParse<UniversityType>(x.Type, true, out var t) ? t : UniversityType.Public
            }));
        await db.SaveChangesAsync(cancellationToken);

        db.UniversityFaculties.AddRange(validUniversityFaculties.Select(x => new UniversityFaculty
        {
            Id = x.Id,
            UniversityId = x.UniversityId,
            FacultyId = x.FacultyId
        }));
        db.AdmissionYears.AddRange(years.Select(x => new AdmissionYear
        {
            Id = x.Id,
            Year = x.Year,
            MaximumScore = x.MaximumScore,
            IsCurrent = x.IsCurrent
        }));
        await db.SaveChangesAsync(cancellationToken);

        db.AdmissionCutoffs.AddRange(validCutoffs.Select(x => new AdmissionCutoff
        {
            Id = x.Id,
            AdmissionYearId = x.AdmissionYearId,
            UniversityFacultyId = x.UniversityFacultyId,
            Track = CutoffSeedLoader.TrackParse(x.Track),
            CutoffScore = x.CutoffScore
        }));
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Database seeded successfully with {Universities} universities, {UniversityFaculties} university-faculties, and {Cutoffs} cutoffs.",
            universities.Count,
            validUniversityFaculties.Count,
            validCutoffs.Count);
    }

    private async Task SyncCurrentAdmissionYearFromSeedAsync(CancellationToken cancellationToken)
    {
        var dataPath = SeedDataPathResolver.Resolve(env);
        if (dataPath is null)
            return;

        var years = await ReadJsonAsync<SeedAdmissionYear>(
            Path.Combine(dataPath, "AdmissionYears.json"),
            cancellationToken);
        var seedCurrent = years.FirstOrDefault(x => x.IsCurrent);
        if (seedCurrent is null)
            return;

        var dbCurrent = await db.AdmissionYears
            .FirstOrDefaultAsync(x => x.IsCurrent, cancellationToken);
        if (dbCurrent is null)
            return;

        if (dbCurrent.Year == seedCurrent.Year &&
            dbCurrent.MaximumScore == seedCurrent.MaximumScore)
            return;

        logger.LogInformation(
            "Syncing current admission year from seed: {OldYear} -> {NewYear}.",
            dbCurrent.Year,
            seedCurrent.Year);

        dbCurrent.Year = seedCurrent.Year;
        dbCurrent.MaximumScore = seedCurrent.MaximumScore;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Faculty MapFaculty(SeedFaculty seed) => new()
    {
        Id = seed.Id,
        NameAr = seed.NameAr,
        AllowedTracks = (seed.AllowedTracks ?? [])
            .Select(CutoffSeedLoader.TrackParse)
            .Distinct()
            .ToList()
    };

    private async Task ClearBusinessDataAsync(CancellationToken ct)
    {
        await db.AdmissionCutoffs.ExecuteDeleteAsync(ct);
        await db.AdmissionYears.ExecuteDeleteAsync(ct);
        await db.UniversityFaculties.ExecuteDeleteAsync(ct);
        await db.Universities.ExecuteDeleteAsync(ct);
        await db.Faculties.ExecuteDeleteAsync(ct);
        await db.Governorates.ExecuteDeleteAsync(ct);
    }

    private static async Task<List<T>> ReadJsonAsync<T>(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var result = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, ct);
        return result ?? [];
    }

    private record SeedGovernorate(int Id, string NameAr);
    private record SeedFaculty(int Id, string NameAr, List<string>? AllowedTracks);
    private record SeedUniversity(int Id, string NameAr, int GovernorateId, string Type);
    private record SeedUniversityFaculty(int Id, int UniversityId, int FacultyId);
    private record SeedAdmissionYear(int Id, int Year, decimal MaximumScore, bool IsCurrent);
    private record SeedCutoff(int Id, int AdmissionYearId, int UniversityFacultyId, string Track, decimal CutoffScore);
}
