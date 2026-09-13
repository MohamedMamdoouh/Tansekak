using Tansekak.Application.Common;
using Tansekak.Domain.Enums;

namespace Tansekak.Application.Tests;

public class StudentTrackMatchRulesTests
{
    [Theory]
    [InlineData("علمي علوم", "2712345", AcademicTrack.Science)]
    [InlineData("علمي رياضه", "2543210", AcademicTrack.Mathematics)]
    [InlineData("الادبي", "2012345", AcademicTrack.Literature)]
    [InlineData(null, "2789012", AcademicTrack.Science)]
    [InlineData("", "2432109", AcademicTrack.Mathematics)]
    public void MatchesTrack_uses_case_desc_or_seating_fallback(
        string? caseDesc,
        string seatingNo,
        AcademicTrack expected)
    {
        var matches = StudentTrackMatchRules.MatchesTrack(null, caseDesc, seatingNo, expected);
        Assert.True(matches);
    }

    [Fact]
    public void MatchesTrack_prefers_explicit_track_over_inference()
    {
        var matches = StudentTrackMatchRules.MatchesTrack(
            AcademicTrack.Literature,
            "علمي علوم",
            "2789012",
            AcademicTrack.Science);

        Assert.False(matches);
    }

    [Fact]
    public void ResolveTrack_and_match_rules_stay_consistent_for_science()
    {
        var entity = new Tansekak.Domain.Entities.StudentResult
        {
            SeatingNo = "2789012",
            StudentCaseDesc = "غير محدد",
            TotalDegree = 300,
            ArabicName = "Test",
            AdmissionYearId = 1
        };

        var resolved = StudentTrackRankCalculator.ResolveTrack(entity);
        Assert.Equal(AcademicTrack.Science, resolved);
        Assert.True(StudentTrackMatchRules.MatchesTrack(
            entity.Track,
            entity.StudentCaseDesc,
            entity.SeatingNo,
            AcademicTrack.Science));
    }
}
