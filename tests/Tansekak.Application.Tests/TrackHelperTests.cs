using Tansekak.Application.Common;
using Tansekak.Domain.Enums;

namespace Tansekak.Application.Tests;

public class TrackHelperTests
{
    [Theory]
    [InlineData("ادبي", AcademicTrack.Literature)]
    [InlineData("الادبي", AcademicTrack.Literature)]
    [InlineData("الشعبة الأدبية", AcademicTrack.Literature)]
    [InlineData("علمي رياضه", AcademicTrack.Science)]
    [InlineData("علمي رياضة", AcademicTrack.Science)]
    [InlineData("Mathematics", AcademicTrack.Science)]
    [InlineData("علمي علوم", AcademicTrack.Science)]
    [InlineData("علمي", AcademicTrack.Science)]
    [InlineData("الشعبة العلمية", AcademicTrack.Science)]
    [InlineData("Science", AcademicTrack.Science)]
    [InlineData("Literature", AcademicTrack.Literature)]
    public void TryParse_accepts_normalized_arabic_variants(string input, AcademicTrack expected)
    {
        var parsed = TrackHelper.TryParse(input, out var track);
        Assert.True(parsed);
        Assert.Equal(expected, track);
    }

    [Fact]
    public void AllTracks_exposes_science_and_literature_only()
    {
        Assert.Equal(["Science", "Literature"], TrackHelper.AllTracks);
    }

    [Fact]
    public void Canonical_maps_mathematics_to_science()
    {
        Assert.Equal(AcademicTrack.Science, TrackHelper.Canonical(AcademicTrack.Mathematics));
        Assert.Equal(AcademicTrack.Science, TrackHelper.Canonical(AcademicTrack.Science));
        Assert.Equal(AcademicTrack.Literature, TrackHelper.Canonical(AcademicTrack.Literature));
    }

    [Fact]
    public void ToArabicName_uses_two_track_labels()
    {
        Assert.Equal("الشعبة العلمية", TrackHelper.ToArabicName(AcademicTrack.Science));
        Assert.Equal("الشعبة العلمية", TrackHelper.ToArabicName(AcademicTrack.Mathematics));
        Assert.Equal("الشعبة الأدبية", TrackHelper.ToArabicName(AcademicTrack.Literature));
    }

    [Fact]
    public void ToDisplayName_maps_mathematics_to_science()
    {
        Assert.Equal("Science", TrackHelper.ToDisplayName(AcademicTrack.Mathematics));
        Assert.Equal("Science", TrackHelper.ToDisplayName(AcademicTrack.Science));
        Assert.Equal("Literature", TrackHelper.ToDisplayName(AcademicTrack.Literature));
    }
}
