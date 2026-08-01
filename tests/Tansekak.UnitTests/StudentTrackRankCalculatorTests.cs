using Tansekak.Application.Common;

namespace Tansekak.UnitTests;

public class StudentTrackRankCalculatorTests
{
    [Theory]
    [InlineData(300, "1001", 295, "1005", false)]
    [InlineData(295, "1005", 300, "1001", true)]
    [InlineData(295, "1005", 295, "1002", true)]
    [InlineData(295, "1002", 295, "1005", false)]
    [InlineData(295, "1005", 295, "1005", false)]
    public void RanksHigher_UsesScoreThenSeatingNo(
        decimal score,
        string seatingNo,
        decimal peerScore,
        string peerSeatingNo,
        bool expected)
    {
        var actual = StudentTrackRankCalculator.RanksHigher(
            score,
            seatingNo,
            peerScore,
            peerSeatingNo);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ResolveTrack_UsesStoredTrack_WhenPresent()
    {
        var entity = new Tansekak.Domain.Entities.StudentResult
        {
            Track = Tansekak.Domain.Enums.AcademicTrack.Mathematics,
            StudentCaseDesc = "ناجح - علمي علوم"
        };

        var track = StudentTrackRankCalculator.ResolveTrack(entity);

        Assert.Equal(Tansekak.Domain.Enums.AcademicTrack.Mathematics, track);
    }

    [Fact]
    public void ResolveTrack_InfersFromCaseDesc_WhenTrackMissing()
    {
        var entity = new Tansekak.Domain.Entities.StudentResult
        {
            StudentCaseDesc = "ناجح - علمي علوم"
        };

        var track = StudentTrackRankCalculator.ResolveTrack(entity);

        Assert.Equal(Tansekak.Domain.Enums.AcademicTrack.Science, track);
    }

    [Fact]
    public void ResolveTrack_ReturnsNull_WhenTrackUnknown()
    {
        var entity = new Tansekak.Domain.Entities.StudentResult
        {
            StudentCaseDesc = "ناجح"
        };

        var track = StudentTrackRankCalculator.ResolveTrack(entity);

        Assert.Null(track);
    }
}
