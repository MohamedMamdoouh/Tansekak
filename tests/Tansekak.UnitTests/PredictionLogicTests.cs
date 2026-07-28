namespace Tansekak.UnitTests;

public class PredictionLogicTests
{
    [Theory]
    [InlineData(286, 286, true)]
    [InlineData(286, 285.5, true)]
    [InlineData(286, 286.5, false)]
    [InlineData(286, 287, false)]
    public void FiltersAvailableOnly(decimal studentScore, decimal cutoff, bool isEligible)
    {
        var eligible = studentScore >= cutoff;
        Assert.Equal(isEligible, eligible);
    }

    [Fact]
    public void SortsByAbsoluteDifferenceAscending()
    {
        var items = new[] { 287m, 285.5m, 286.5m, 286m };
        var score = 286m;
        var sorted = items.OrderBy(x => Math.Abs(score - x)).ToArray();
        Assert.Equal(new[] { 286m, 285.5m, 286.5m, 287m }, sorted);
    }
}
