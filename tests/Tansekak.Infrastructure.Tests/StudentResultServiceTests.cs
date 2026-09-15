using Microsoft.EntityFrameworkCore;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class StudentResultServiceTests
{
    [Fact]
    public async Task GetBySeatingNoAsync_returns_track_rank_for_persisted_science_track()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            db.AdmissionYears.Add(new AdmissionYear
            {
                Id = 1,
                Year = 2026,
                MaximumScore = 410,
                IsCurrent = true
            });
            db.StudentResults.AddRange(
                new StudentResult
                {
                    Id = 1,
                    AdmissionYearId = 1,
                    SeatingNo = "2410001",
                    ArabicName = "طالب أ",
                    TotalDegree = 390,
                    StudentCaseDesc = "علمي علوم",
                    Track = AcademicTrack.Science
                },
                new StudentResult
                {
                    Id = 2,
                    AdmissionYearId = 1,
                    SeatingNo = "2410002",
                    ArabicName = "طالب ب",
                    TotalDegree = 380,
                    StudentCaseDesc = "علمي علوم",
                    Track = AcademicTrack.Science
                },
                new StudentResult
                {
                    Id = 3,
                    AdmissionYearId = 1,
                    SeatingNo = "2010001",
                    ArabicName = "طالب ج",
                    TotalDegree = 400,
                    StudentCaseDesc = "ادبي",
                    Track = AcademicTrack.Literature
                });
            await db.SaveChangesAsync();

            var service = new StudentResultService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.GetBySeatingNoAsync("2410002");

            Assert.NotNull(result);
            Assert.Equal(410, result.MaximumScore);
            Assert.Equal("Science", result.Track);
            Assert.Equal(2, result.TrackRank);
            Assert.Equal(2, result.TrackTotalStudents);
        }
    }

    [Fact]
    public async Task GetBySeatingNoAsync_ranks_science_and_mathematics_separately()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            db.AdmissionYears.Add(new AdmissionYear
            {
                Id = 1,
                Year = 2026,
                MaximumScore = 410,
                IsCurrent = true
            });
            db.StudentResults.AddRange(
                new StudentResult
                {
                    Id = 1,
                    AdmissionYearId = 1,
                    SeatingNo = "2710001",
                    ArabicName = "طالب علوم",
                    TotalDegree = 390,
                    StudentCaseDesc = "علمي علوم",
                    Track = AcademicTrack.Science
                },
                new StudentResult
                {
                    Id = 2,
                    AdmissionYearId = 1,
                    SeatingNo = "2510002",
                    ArabicName = "طالب رياضة",
                    TotalDegree = 390,
                    StudentCaseDesc = "علمي رياضة",
                    Track = AcademicTrack.Mathematics
                });
            await db.SaveChangesAsync();

            var service = new StudentResultService(db, new CurrentAdmissionYearProvider(db));
            var science = await service.GetBySeatingNoAsync("2710001");
            var mathematics = await service.GetBySeatingNoAsync("2510002");

            Assert.NotNull(science);
            Assert.NotNull(mathematics);
            Assert.Equal("Science", science!.Track);
            Assert.Equal("Mathematics", mathematics!.Track);
            Assert.Equal(1, science.TrackRank);
            Assert.Equal(1, mathematics.TrackRank);
            Assert.Equal(1, science.TrackTotalStudents);
            Assert.Equal(1, mathematics.TrackTotalStudents);
        }
    }

    [Fact]
    public async Task GetBySeatingNoAsync_ranks_peers_with_persisted_track()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            db.AdmissionYears.Add(new AdmissionYear
            {
                Id = 1,
                Year = 2026,
                MaximumScore = 410,
                IsCurrent = true
            });
            db.StudentResults.AddRange(
                new StudentResult
                {
                    Id = 1,
                    AdmissionYearId = 1,
                    SeatingNo = "2410001",
                    ArabicName = "طالب أ",
                    TotalDegree = 390,
                    StudentCaseDesc = "علمي علوم",
                    Track = AcademicTrack.Science
                },
                new StudentResult
                {
                    Id = 2,
                    AdmissionYearId = 1,
                    SeatingNo = "2510002",
                    ArabicName = "طالب ب",
                    TotalDegree = 370,
                    StudentCaseDesc = "",
                    Track = AcademicTrack.Science
                });
            await db.SaveChangesAsync();

            var service = new StudentResultService(db, new CurrentAdmissionYearProvider(db));
            var result = await service.GetBySeatingNoAsync("2510002");

            Assert.NotNull(result);
            Assert.Equal("Science", result.Track);
            Assert.Equal(2, result.TrackRank);
            Assert.Equal(2, result.TrackTotalStudents);
        }
    }
}
