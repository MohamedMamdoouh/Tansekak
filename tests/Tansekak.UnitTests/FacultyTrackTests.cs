using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

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
