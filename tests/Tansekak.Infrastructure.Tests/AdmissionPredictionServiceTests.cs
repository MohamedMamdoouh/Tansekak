using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class AdmissionPredictionServiceTests
{
    [Fact]
    public async Task Science_request_includes_science_faculties_only()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.PredictAsync(new PredictRequestDto("Science", 380));

            Assert.Single(result.Results);
            Assert.Equal("طب", result.Results[0].Faculty.NameAr);
        }
    }

    [Fact]
    public async Task Mathematics_request_includes_mathematics_faculties_only()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.PredictAsync(new PredictRequestDto("Mathematics", 380));

            Assert.Single(result.Results);
            Assert.Equal("هندسة", result.Results[0].Faculty.NameAr);
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
            var result = await service.PredictAsync(new PredictRequestDto("Mathematics", 300));

            Assert.Single(result.Results);
            Assert.Equal("هندسة", result.Results[0].Faculty.NameAr);
        }
    }

    [Theory]
    [InlineData("Mathematics")]
    [InlineData("علمي رياضة")]
    public async Task Mathematics_aliases_match_mathematics_results(string track)
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var mathematics = await service.PredictAsync(new PredictRequestDto("Mathematics", 380));
            var aliased = await service.PredictAsync(new PredictRequestDto(track, 380));

            Assert.Equal(mathematics.TotalCount, aliased.TotalCount);
            Assert.Equal(
                mathematics.Results.Select(r => r.Faculty.NameAr).OrderBy(x => x),
                aliased.Results.Select(r => r.Faculty.NameAr).OrderBy(x => x));
        }
    }

    [Fact]
    public async Task Science_and_mathematics_requests_return_different_faculties()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var science = await service.PredictAsync(new PredictRequestDto("Science", 380));
            var mathematics = await service.PredictAsync(new PredictRequestDto("Mathematics", 380));

            Assert.Contains(science.Results, r => r.Faculty.NameAr == "طب");
            Assert.DoesNotContain(science.Results, r => r.Faculty.NameAr == "هندسة");
            Assert.Contains(mathematics.Results, r => r.Faculty.NameAr == "هندسة");
            Assert.DoesNotContain(mathematics.Results, r => r.Faculty.NameAr == "طب");
        }
    }

    [Fact]
    public async Task Pagination_returns_has_more_and_requested_page_size()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            db.Faculties.Add(new Faculty
            {
                Id = 3,
                NameAr = "صيدلة",
                AllowedTracks = [AcademicTrack.Science]
            });
            db.UniversityFaculties.Add(new UniversityFaculty { Id = 3, UniversityId = 1, FacultyId = 3 });
            db.AdmissionCutoffs.Add(new AdmissionCutoff
            {
                Id = 3,
                AdmissionYearId = 1,
                UniversityFacultyId = 3,
                Track = AcademicTrack.Science,
                CutoffScore = 360
            });
            await db.SaveChangesAsync();

            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.PredictAsync(new PredictRequestDto("Science", 380, Page: 1, PageSize: 1));

            Assert.Equal(2, result.TotalCount);
            Assert.Single(result.Results);
            Assert.True(result.HasMore);
        }
    }

    [Fact]
    public async Task Score_above_maximum_throws_score_exceeds_max()
    {
        var (db, connection) = await SeedAsync();
        await using (connection)
        await using (db)
        {
            var service = new AdmissionPredictionService(db, new CurrentAdmissionYearProvider(db));

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => service.PredictAsync(new PredictRequestDto("Science", 500)));

            Assert.Equal(ApiErrorCodes.ScoreExceedsMax, ex.ErrorCode);
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
