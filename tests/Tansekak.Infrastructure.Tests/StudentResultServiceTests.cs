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
            Assert.Equal("Science", result.Track);
            Assert.Equal(2, result.TrackRank);
            Assert.Equal(2, result.TrackTotalStudents);
        }
    }

    [Fact]
    public async Task GetBySeatingNoAsync_ranks_null_track_peers_via_case_desc_and_seating()
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
                    Track = null
                },
                new StudentResult
                {
                    Id = 2,
                    AdmissionYearId = 1,
                    SeatingNo = "2510002",
                    ArabicName = "طالب ب",
                    TotalDegree = 370,
                    StudentCaseDesc = "",
                    Track = null
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
