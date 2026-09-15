namespace Tansekak.Infrastructure.Seeding;

public static class SeedDirectoryLocator
{
    public static string Resolve()
    {
        var fromOutput = Path.Combine(AppContext.BaseDirectory, "SeedData");
        if (IsCompleteSeedDirectory(fromOutput))
            return fromOutput;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "SeededData");
            if (IsCompleteSeedDirectory(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Seed data directory was not found. Expected SeedData next to the application or SeededData in the repository root.");
    }

    public static string? TryResolveCutoffsDirectory()
    {
        try
        {
            var seedDirectory = Resolve();
            var cutoffsDirectory = Path.Combine(seedDirectory, "cutoffs");
            return Directory.Exists(cutoffsDirectory) ? cutoffsDirectory : null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    private static bool IsCompleteSeedDirectory(string path) =>
        File.Exists(Path.Combine(path, SeedFileNames.Faculties))
        && File.Exists(Path.Combine(path, SeedFileNames.Governorates));
}
