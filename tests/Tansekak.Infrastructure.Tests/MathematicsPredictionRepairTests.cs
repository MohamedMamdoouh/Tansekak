using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tansekak.Application.DTOs;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Seeding;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class MathematicsPredictionRepairTests
{
    [Fact]
    public async Task Collapsed_mathematics_data_is_repaired_and_predicts_colleges_for_score_300()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedCatalog(db);
            db.AdmissionYears.Add(new AdmissionYear
            {
                Id = 1,
                Year = 2026,
                MaximumScore = 410,
                IsCurrent = true,
            });
            await db.SaveChangesAsync();

            await SimulateCollapseMigrationAsync(db);

            var predictionBeforeRepair = new AdmissionPredictionService(
                db,
                new CurrentAdmissionYearProvider(db));
            var beforeRepair = await predictionBeforeRepair.PredictAsync(
                new PredictRequestDto("Mathematics", 300));
            Assert.Equal(0, beforeRepair.TotalCount);

            var seedDirectory = SeededDataDirectory();
            var seedData = new SeedFileSystemReader(seedDirectory);
            var facultyRepair = new FacultyAllowedTracksRepairService(
                db,
                seedData,
                NullLogger<FacultyAllowedTracksRepairService>.Instance);
            var repairedFaculties = await facultyRepair.RepairAsync();
            Assert.True(repairedFaculties > 0);

            var importService = new ImportService(db, new EntityIdAllocator(db), NullLogger<ImportService>.Instance);
            var cutoffBootstrap = new CutoffBootstrapService(
                db,
                importService,
                new CurrentAdmissionYearProvider(db),
                NullLogger<CutoffBootstrapService>.Instance);
            var importedTracks = await cutoffBootstrap.BootstrapMissingTracksAsync();
            Assert.Equal(3, importedTracks);

            var mathematicsCutoffs = await db.AdmissionCutoffs.CountAsync(
                cutoff => cutoff.Track == AcademicTrack.Mathematics);
            Assert.True(mathematicsCutoffs >= 80);

            var engineering = await db.Faculties.SingleAsync(faculty => faculty.NameAr == "هندسة");
            Assert.Contains(AcademicTrack.Mathematics, engineering.AllowedTracks);

            var predictionAfterRepair = new AdmissionPredictionService(
                db,
                new CurrentAdmissionYearProvider(db));
            var afterRepair = await predictionAfterRepair.PredictAsync(
                new PredictRequestDto("Mathematics", 300));

            Assert.True(afterRepair.TotalCount >= 80);
            Assert.Contains(afterRepair.Results, result => result.Faculty.NameAr == "هندسة");
        }
    }

    private static async Task SimulateCollapseMigrationAsync(AppDbContext db)
    {
        var mathematicsCutoffs = await db.AdmissionCutoffs
            .Where(cutoff => cutoff.Track == AcademicTrack.Mathematics)
            .ToListAsync();
        db.AdmissionCutoffs.RemoveRange(mathematicsCutoffs);

        var faculties = await db.Faculties.ToListAsync();
        foreach (var faculty in faculties)
        {
            faculty.AllowedTracks = faculty.AllowedTracks
                .Select(track => track == AcademicTrack.Mathematics ? AcademicTrack.Science : track)
                .Distinct()
                .ToList();
        }

        await db.SaveChangesAsync();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static void SeedCatalog(AppDbContext db)
    {
        var seedDir = SeededDataDirectory();
        var governorates = JsonSerializer.Deserialize<List<SeedGovernorate>>(
            File.ReadAllText(Path.Combine(seedDir, "Governorates.json")), JsonOptions)!;
        var faculties = JsonSerializer.Deserialize<List<SeedFaculty>>(
            File.ReadAllText(Path.Combine(seedDir, "Faculties.json")), JsonOptions)!;
        var universities = JsonSerializer.Deserialize<List<SeedUniversity>>(
            File.ReadAllText(Path.Combine(seedDir, "Universities.json")), JsonOptions)!;
        var links = JsonSerializer.Deserialize<List<SeedUniversityFaculty>>(
            File.ReadAllText(Path.Combine(seedDir, "UniversityFaculties.json")), JsonOptions)!;

        var governorateIds = governorates.Select(governorate => governorate.Id).ToHashSet();
        var facultyIds = faculties.Select(faculty => faculty.Id).ToHashSet();
        var universityIds = universities.Select(university => university.Id).ToHashSet();

        db.Governorates.AddRange(governorates.Select(governorate => new Governorate
        {
            Id = governorate.Id,
            NameAr = governorate.NameAr,
        }));
        db.Faculties.AddRange(faculties.Select(FacultySeedMapper.MapFaculty));
        db.Universities.AddRange(universities
            .Where(university => governorateIds.Contains(university.GovernorateId))
            .Select(university => new University
            {
                Id = university.Id,
                NameAr = university.NameAr,
                GovernorateId = university.GovernorateId,
                Type = Enum.TryParse<UniversityType>(university.Type, true, out var type)
                    ? type
                    : UniversityType.Public,
            }));
        db.UniversityFaculties.AddRange(links
            .Where(link => universityIds.Contains(link.UniversityId) && facultyIds.Contains(link.FacultyId))
            .Select(link => new UniversityFaculty
            {
                Id = link.Id,
                UniversityId = link.UniversityId,
                FacultyId = link.FacultyId,
            }));
        db.SaveChanges();
    }

    private static string SeededDataDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "SeededData");
            if (Directory.Exists(candidate)
                && File.Exists(Path.Combine(candidate, "Faculties.json"))
                && Directory.Exists(Path.Combine(candidate, "cutoffs")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("SeededData directory was not found.");
    }
}
