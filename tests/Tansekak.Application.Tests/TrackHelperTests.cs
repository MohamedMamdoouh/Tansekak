using Tansekak.Application.Common;
using Tansekak.Domain.Enums;

namespace Tansekak.Application.Tests;

public class TrackHelperTests
{
    [Theory]
    [InlineData("ادبي", AcademicTrack.Literature)]
    [InlineData("الادبي", AcademicTrack.Literature)]
    [InlineData("علمي رياضه", AcademicTrack.Mathematics)]
    [InlineData("علمي رياضة", AcademicTrack.Mathematics)]
    [InlineData("Mathematics", AcademicTrack.Mathematics)]
    [InlineData("علمي علوم", AcademicTrack.Science)]
    [InlineData("Science", AcademicTrack.Science)]
    [InlineData("Literature", AcademicTrack.Literature)]
    public void TryParse_accepts_normalized_arabic_variants(string input, AcademicTrack expected)
    {
        var parsed = TrackHelper.TryParse(input, out var track);
        Assert.True(parsed);
        Assert.Equal(expected, track);
    }

    [Fact]
    public void AllTracks_exposes_three_tracks()
    {
        Assert.Equal(["Science", "Mathematics", "Literature"], TrackHelper.AllTracks);
    }

    [Fact]
    public void ToArabicName_uses_three_track_labels()
    {
        Assert.Equal("علمي علوم", TrackHelper.ToArabicName(AcademicTrack.Science));
        Assert.Equal("علمي رياضة", TrackHelper.ToArabicName(AcademicTrack.Mathematics));
        Assert.Equal("أدبي", TrackHelper.ToArabicName(AcademicTrack.Literature));
    }

    [Fact]
    public void ToDisplayName_preserves_distinct_tracks()
    {
        Assert.Equal("Science", TrackHelper.ToDisplayName(AcademicTrack.Science));
        Assert.Equal("Mathematics", TrackHelper.ToDisplayName(AcademicTrack.Mathematics));
        Assert.Equal("Literature", TrackHelper.ToDisplayName(AcademicTrack.Literature));
    }
}
