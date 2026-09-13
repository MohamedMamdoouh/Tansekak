using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Common;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Seeding;

/// <summary>
/// Bootstraps the database from the seed files exactly once. Once Governorates has any rows,
/// this is a no-op forever after - the database is the sole source of truth for business data
/// from that point on, managed only through the admin CRUD/import APIs.
/// </summary>
public class JsonSeedService(
    AppDbContext db,
    ISeedDataSource seedData,
    ILogger<JsonSeedService> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Governorates.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already bootstrapped. Skipping seed.");
            return;
        }

        logger.LogInformation("Loading seed data from disk.");
        var governorates = await seedData.ReadListAsync<SeedGovernorate>(SeedFileNames.Governorates, cancellationToken);
        var faculties = await seedData.ReadListAsync<SeedFaculty>(SeedFileNames.Faculties, cancellationToken);
        var universities = await seedData.ReadListAsync<SeedUniversity>(SeedFileNames.Universities, cancellationToken);
        var universityFaculties = await seedData.ReadListAsync<SeedUniversityFaculty>(SeedFileNames.UniversityFaculties, cancellationToken);
        var bootstrapYear = AdmissionDefaults.BootstrapYear();

        var governorateIds = governorates.Select(x => x.Id).ToHashSet();
        var facultyIds = faculties.Select(x => x.Id).ToHashSet();
        var universityIds = universities.Select(x => x.Id).ToHashSet();

        var validUniversityFaculties = universityFaculties
            .Where(x => universityIds.Contains(x.UniversityId) && facultyIds.Contains(x.FacultyId))
            .ToList();

        var skippedUf = universityFaculties.Count - validUniversityFaculties.Count;
        if (skippedUf > 0)
            logger.LogWarning("Skipped {Count} university-faculty rows with invalid references.", skippedUf);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        db.Governorates.AddRange(governorates.Select(x => new Governorate { Id = x.Id, NameAr = x.NameAr }));
        db.Faculties.AddRange(faculties.Select(FacultySeedMapper.MapFaculty));
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
        db.AdmissionYears.Add(new AdmissionYear
        {
            Id = 1,
            Year = bootstrapYear,
            MaximumScore = AdmissionDefaults.MaximumScore,
            IsCurrent = true
        });
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Database bootstrapped for admission year {Year} (maximum score {MaximumScore}) with {Universities} universities and {UniversityFaculties} university-faculties. Upload cutoffs via admin import.",
            bootstrapYear,
            AdmissionDefaults.MaximumScore,
            universities.Count,
            validUniversityFaculties.Count);
    }
}
