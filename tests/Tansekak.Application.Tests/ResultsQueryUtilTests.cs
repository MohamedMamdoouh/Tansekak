namespace Tansekak.Application.Tests;

/// <summary>
/// Mirrors client/src/app/utils/results-query.util.ts validation rules.
/// </summary>
public class ResultsQueryUtilTests
{
    private static bool IsValidResultsQuery(string track, double score) =>
        !string.IsNullOrWhiteSpace(track) && !double.IsNaN(score);

    private static double ParseResultsScore(string? rawScore)
    {
        if (string.IsNullOrWhiteSpace(rawScore))
            return double.NaN;
        return double.Parse(rawScore);
    }

    [Theory]
    [InlineData("Science", 0, true)]
    [InlineData("Science", 250.5, true)]
    [InlineData("", 250, false)]
    [InlineData("Science", double.NaN, false)]
    public void Results_query_validation_matches_client_rules(
        string track,
        double score,
        bool expected)
    {
        Assert.Equal(expected, IsValidResultsQuery(track, score));
    }

    [Fact]
    public void ParseResultsScore_treats_missing_value_as_invalid()
    {
        Assert.True(double.IsNaN(ParseResultsScore(null)));
        Assert.True(double.IsNaN(ParseResultsScore("")));
    }
}
