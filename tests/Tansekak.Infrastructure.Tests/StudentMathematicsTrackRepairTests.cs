using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Seeding;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class StudentMathematicsTrackRepairTests
{
    [Fact]
    public async Task RepairAsync_restores_mathematics_collapsed_to_science_and_fixes_track_rank()
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
                    // Collapse migration rewrote Mathematics → Science; CaseDesc still says Math.
                    StudentCaseDesc = "علمي رياضة",
                    Track = AcademicTrack.Science
                },
                new StudentResult
                {
                    Id = 3,
                    AdmissionYearId = 1,
                    SeatingNo = "2610003",
                    ArabicName = "طالب رياضة بلا وصف",
                    TotalDegree = 380,
                    StudentCaseDesc = "",
                    // Seating 2nd digit 6 ⇒ Mathematics; stored Science after collapse.
                    Track = AcademicTrack.Science
                });
            await db.SaveChangesAsync();

            var before = new StudentResultService(db, new CurrentAdmissionYearProvider(db));
            var collapsed = await before.GetBySeatingNoAsync("2510002");
            Assert.NotNull(collapsed);
            Assert.Equal("Science", collapsed!.Track);
            // All three rows still stored as Science after the collapse rewrite.
            Assert.Equal(3, collapsed.TrackTotalStudents);

            var repair = new StudentMathematicsTrackRepairService(
                db,
                NullLogger<StudentMathematicsTrackRepairService>.Instance);
            var repaired = await repair.RepairAsync();
            Assert.Equal(2, repaired);

            var mathFromCase = await db.StudentResults.SingleAsync(r => r.Id == 2);
            var mathFromSeating = await db.StudentResults.SingleAsync(r => r.Id == 3);
            var science = await db.StudentResults.SingleAsync(r => r.Id == 1);
            Assert.Equal(AcademicTrack.Mathematics, mathFromCase.Track);
            Assert.Equal(AcademicTrack.Mathematics, mathFromSeating.Track);
            Assert.Equal(AcademicTrack.Science, science.Track);

            var after = new StudentResultService(db, new CurrentAdmissionYearProvider(db));
            var mathematics = await after.GetBySeatingNoAsync("2510002");
            var trueScience = await after.GetBySeatingNoAsync("2710001");

            Assert.NotNull(mathematics);
            Assert.NotNull(trueScience);
            Assert.Equal("Mathematics", mathematics!.Track);
            Assert.Equal("Science", trueScience!.Track);
            Assert.Equal(1, mathematics.TrackRank);
            Assert.Equal(2, mathematics.TrackTotalStudents);
            Assert.Equal(1, trueScience.TrackTotalStudents);
        }
    }

    [Fact]
    public async Task RepairAsync_is_idempotent_and_skips_true_science()
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
            db.StudentResults.Add(new StudentResult
            {
                Id = 1,
                AdmissionYearId = 1,
                SeatingNo = "2710001",
                ArabicName = "طالب علوم",
                TotalDegree = 390,
                StudentCaseDesc = "علمي علوم",
                Track = AcademicTrack.Science
            });
            await db.SaveChangesAsync();

            var repair = new StudentMathematicsTrackRepairService(
                db,
                NullLogger<StudentMathematicsTrackRepairService>.Instance);

            Assert.Equal(0, await repair.RepairAsync());
            Assert.Equal(0, await repair.RepairAsync());
            Assert.Equal(
                AcademicTrack.Science,
                (await db.StudentResults.SingleAsync()).Track);
        }
    }

    [Fact]
    public async Task RepairAsync_case_desc_science_wins_over_math_seating()
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
            // Prefilter may load this (Math seating), but CaseDesc must keep Science.
            db.StudentResults.Add(new StudentResult
            {
                Id = 1,
                AdmissionYearId = 1,
                SeatingNo = "2519999",
                ArabicName = "طالب علوم بمقعد رياضة",
                TotalDegree = 390,
                StudentCaseDesc = "علمي علوم",
                Track = AcademicTrack.Science
            });
            await db.SaveChangesAsync();

            var repair = new StudentMathematicsTrackRepairService(
                db,
                NullLogger<StudentMathematicsTrackRepairService>.Instance);

            Assert.Equal(0, await repair.RepairAsync());
            Assert.Equal(
                AcademicTrack.Science,
                (await db.StudentResults.SingleAsync()).Track);
        }
    }

    [Fact]
    public async Task RepairAsync_does_not_touch_large_true_science_cohort()
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

            // Many true Science rows with Science seating — must not be rewritten.
            var scienceRows = Enumerable.Range(0, 250)
                .Select(i => new StudentResult
                {
                    Id = i + 1,
                    AdmissionYearId = 1,
                    SeatingNo = $"27{i:D5}",
                    ArabicName = $"طالب {i}",
                    TotalDegree = 300 + (i % 50),
                    StudentCaseDesc = "علمي علوم",
                    Track = AcademicTrack.Science
                })
                .ToList();
            db.StudentResults.AddRange(scienceRows);
            await db.SaveChangesAsync();

            var repair = new StudentMathematicsTrackRepairService(
                db,
                NullLogger<StudentMathematicsTrackRepairService>.Instance);

            Assert.Equal(0, await repair.RepairAsync());
            Assert.Equal(
                250,
                await db.StudentResults.CountAsync(r => r.Track == AcademicTrack.Science));
            Assert.Equal(
                0,
                await db.StudentResults.CountAsync(r => r.Track == AcademicTrack.Mathematics));
        }
    }
}
