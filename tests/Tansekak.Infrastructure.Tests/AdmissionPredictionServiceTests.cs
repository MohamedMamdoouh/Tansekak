using Tansekak.Application.DTOs;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class AdmissionPredictionServiceTests
{
    [Fact]
    public async Task Science_request_includes_former_science_and_mathematics_faculties()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.PredictAsync(new PredictRequestDto("Science", 380));

            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Results, r => r.Faculty.NameAr == "طب");
            Assert.Contains(result.Results, r => r.Faculty.NameAr == "هندسة");
        }
    }

    [Fact]
    public async Task Literature_request_excludes_scientific_faculties()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.PredictAsync(new PredictRequestDto("Literature", 380));

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Results);
        }
    }

    [Fact]
    public async Task Score_below_cutoff_excludes_college()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.PredictAsync(new PredictRequestDto("Science", 300));

            Assert.Single(result.Results);
            Assert.Equal("هندسة", result.Results[0].Faculty.NameAr);
        }
    }

    [Theory]
    [InlineData("Mathematics")]
    [InlineData("علمي رياضة")]
    public async Task Mathematics_aliases_match_science_results(string track)
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var science = await service.PredictAsync(new PredictRequestDto("Science", 380));
            var aliased = await service.PredictAsync(new PredictRequestDto(track, 380));

            Assert.Equal(science.TotalCount, aliased.TotalCount);
            Assert.Equal(
                science.Results.Select(r => r.Faculty.NameAr).OrderBy(x => x),
                aliased.Results.Select(r => r.Faculty.NameAr).OrderBy(x => x));
        }
    }

    [Fact]
    public async Task Duplicate_science_and_mathematics_cutoffs_return_one_college()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            db.AdmissionCutoffs.Add(new AdmissionCutoff
            {
                Id = 3,
                AdmissionYearId = 1,
                UniversityFacultyId = 1,
                Track = AcademicTrack.Mathematics,
                CutoffScore = 370
            });
            await db.SaveChangesAsync();

            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.PredictAsync(new PredictRequestDto("Science", 380));

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Results.Count(r => r.Faculty.NameAr == "طب"));
        }
    }

    private static async Task<(Tansekak.Infrastructure.Persistence.AppDbContext Db, Microsoft.Data.Sqlite.SqliteConnection Connection)> SeedAsync()
    {
        var (db, connection) = TestDbFactory.Create();
        db.AdmissionYears.Add(new AdmissionYear
        {
            Id = 1,
            Year = 2026,
            MaximumScore = 410,
            IsCurrent = true
        });
        db.Governorates.Add(new Governorate { Id = 1, NameAr = "القاهرة" });
        db.Universities.Add(new University
        {
            Id = 1,
            NameAr = "جامعة القاهرة",
            GovernorateId = 1,
            Type = UniversityType.Public
        });
        db.Faculties.AddRange(
            new Faculty
            {
                Id = 1,
                NameAr = "طب",
                AllowedTracks = [AcademicTrack.Science]
            },
            new Faculty
            {
                Id = 2,
                NameAr = "هندسة",
                AllowedTracks = [AcademicTrack.Mathematics]
            });
        db.UniversityFaculties.AddRange(
            new UniversityFaculty { Id = 1, UniversityId = 1, FacultyId = 1 },
            new UniversityFaculty { Id = 2, UniversityId = 1, FacultyId = 2 });
        db.AdmissionCutoffs.AddRange(
            new AdmissionCutoff
            {
                Id = 1,
                AdmissionYearId = 1,
                UniversityFacultyId = 1,
                Track = AcademicTrack.Science,
                CutoffScore = 350
            },
            new AdmissionCutoff
            {
                Id = 2,
                AdmissionYearId = 1,
                UniversityFacultyId = 2,
                Track = AcademicTrack.Mathematics,
                CutoffScore = 280
            });
        await db.SaveChangesAsync();
        return (db, connection);
    }
}
