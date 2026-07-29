using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.UnitTests;

public class FacultyTrackValidatorTests
{
    [Fact]
    public void IsTrackAllowed_Engineering_AllowsMathematicsOnly()
    {
        var faculty = new Faculty
        {
            NameAr = "هندسة",
            AllowedTracks = [AcademicTrack.Mathematics]
        };

        Assert.True(FacultyTrackValidator.IsTrackAllowed(faculty, AcademicTrack.Mathematics));
        Assert.False(FacultyTrackValidator.IsTrackAllowed(faculty, AcademicTrack.Science));
    }

    [Fact]
    public void IsTrackAllowed_ReturnsFalse_WhenAllowedTracksEmpty()
    {
        var faculty = new Faculty
        {
            NameAr = "هندسة",
            AllowedTracks = []
        };

        Assert.False(FacultyTrackValidator.IsTrackAllowed(faculty, AcademicTrack.Mathematics));
    }

    [Fact]
    public void EnsureTrackAllowed_ThrowsForInvalidCombination()
    {
        var faculty = new Faculty
        {
            NameAr = "هندسة",
            AllowedTracks = [AcademicTrack.Mathematics]
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            FacultyTrackValidator.EnsureTrackAllowed(faculty, AcademicTrack.Science));

        Assert.Contains("هندسة", ex.Message);
    }
}

public class StudentTrackInferrerTests
{
    [Theory]
    [InlineData("ناجح - علمي علوم", AcademicTrack.Science)]
    [InlineData("ناجح - علمي رياضة", AcademicTrack.Mathematics)]
    [InlineData("ناجح - أدبي", AcademicTrack.Literature)]
    public void TryInferFromCaseDesc_ParsesKnownTracks(string caseDesc, AcademicTrack expected)
    {
        Assert.Equal(expected, StudentTrackInferrer.TryInferFromCaseDesc(caseDesc));
    }

    [Fact]
    public void TryInferFromCaseDesc_ReturnsNull_WhenUnknown()
    {
        Assert.Null(StudentTrackInferrer.TryInferFromCaseDesc("ناجح"));
    }
}

public class TrackHelperTests
{
    [Theory]
    [InlineData("Mathematics", AcademicTrack.Mathematics)]
    [InlineData("علمي رياضة", AcademicTrack.Mathematics)]
    [InlineData("Science", AcademicTrack.Science)]
    [InlineData("علمي علوم", AcademicTrack.Science)]
    public void TryParse_AcceptsEnglishAndArabicNames(string value, AcademicTrack expected)
    {
        Assert.True(TrackHelper.TryParse(value, out var track));
        Assert.Equal(expected, track);
    }
}

public class FacultyAllowedTracksJsonTests
{
    [Theory]
    [InlineData("[2]", AcademicTrack.Mathematics)]
    [InlineData("[\"Mathematics\"]", AcademicTrack.Mathematics)]
    public void Deserialize_SupportsNumericAndStringEnumValues(string json, AcademicTrack expected)
    {
        var tracks = FacultyAllowedTracksJson.Deserialize(json);
        Assert.Single(tracks);
        Assert.Equal(expected, tracks[0]);
    }

    [Fact]
    public void RoundTrip_PreservesMathematicsTrack()
    {
        var json = FacultyAllowedTracksJson.Serialize([AcademicTrack.Mathematics]);
        var tracks = FacultyAllowedTracksJson.Deserialize(json);
        Assert.Equal([AcademicTrack.Mathematics], tracks);
    }
}

public class CutoffSeedLoaderTests
{
    [Fact]
    public void TrackParse_ThrowsForInvalidValue()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Tansekak.Infrastructure.Seeding.CutoffSeedLoader.TrackParse("InvalidTrack"));

        Assert.Contains("InvalidTrack", ex.Message);
    }
}
