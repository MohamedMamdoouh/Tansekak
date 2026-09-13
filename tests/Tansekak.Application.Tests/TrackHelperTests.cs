using Tansekak.Application.Common;
using Tansekak.Domain.Enums;

namespace Tansekak.Application.Tests;

public class TrackHelperTests
{
    [Theory]
    [InlineData("ادبي", AcademicTrack.Literature)]
    [InlineData("الادبي", AcademicTrack.Literature)]
    [InlineData("علمي رياضه", AcademicTrack.Mathematics)]
    [InlineData("Science", AcademicTrack.Science)]
    public void TryParse_accepts_normalized_arabic_variants(string input, AcademicTrack expected)
    {
        var parsed = TrackHelper.TryParse(input, out var track);
        Assert.True(parsed);
        Assert.Equal(expected, track);
    }
}
