using Tansekak.Infrastructure.Services;

namespace Tansekak.UnitTests;

public class R2ImportStorageServiceTests
{
    [Fact]
    public void EnsureObjectKeyForYear_AcceptsMatchingPrefix()
    {
        var ex = Record.Exception(() =>
            R2ImportStorageService.EnsureObjectKeyForYear("imports/1/abc.xlsx", 1));

        Assert.Null(ex);
    }

    [Fact]
    public void EnsureObjectKeyForYear_RejectsOtherYear()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            R2ImportStorageService.EnsureObjectKeyForYear("imports/2/abc.xlsx", 1));

        Assert.Contains("Invalid object key", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildObjectKey_UsesYearPrefix()
    {
        var key = R2ImportStorageService.BuildObjectKey(2026);

        Assert.StartsWith("imports/2026/", key, StringComparison.Ordinal);
        Assert.EndsWith(".xlsx", key, StringComparison.OrdinalIgnoreCase);
    }
}
