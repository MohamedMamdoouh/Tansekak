namespace Tansekak.Infrastructure.Tests;

public class CutoffSeedSplitTests
{
    [Fact]
    public void Split_merged_science_cutoffs_into_science_and_mathematics_files()
    {
        var seedDirectory = SeededDataDirectory();
        CutoffSeedSplitter.SplitMergedScienceFile(seedDirectory);

        Assert.True(File.Exists(Path.Combine(seedDirectory, "cutoffs", "science-2026.md")));
        Assert.True(File.Exists(Path.Combine(seedDirectory, "cutoffs", "mathematics-2026.md")));
        Assert.True(File.Exists(Path.Combine(seedDirectory, "cutoffs", "science-2026-merged.backup.md")));
    }

    private static string SeededDataDirectory()
    {
        var repoCandidate = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "SeededData"));
        if (Directory.Exists(repoCandidate) && File.Exists(Path.Combine(repoCandidate, "Faculties.json")))
            return repoCandidate;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "SeededData");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "Faculties.json")))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("SeededData directory was not found.");
    }
}
