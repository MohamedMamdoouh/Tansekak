namespace Tansekak.Infrastructure.Seeding;

/// <summary>
/// Reads seed data from plain JSON files on disk rather than compiled resources, so fixing a
/// typo or adding a row to the seed catalog does not require recompiling the assembly.
///
/// The files are shipped as a Content item of the Tansekak.Api project (copied to its own
/// output/publish directory), so they are always found relative to the running assembly's own
/// directory (<see cref="AppContext.BaseDirectory"/>) - this is correct both for `dotnet run`
/// and inside the Docker runtime image, unlike resolving a path relative to the source tree.
/// </summary>
public sealed class SeedFileSystemReader : ISeedDataSource
{
    private readonly string _seedDirectory;

    public SeedFileSystemReader() : this(Path.Combine(AppContext.BaseDirectory, "SeedData"))
    {
    }

    public SeedFileSystemReader(string seedDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedDirectory);
        _seedDirectory = seedDirectory;
    }

    public Task<List<T>> ReadListAsync<T>(string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        cancellationToken.ThrowIfCancellationRequested();

        var path = Path.Combine(_seedDirectory, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Seed file '{fileName}' was not found in '{_seedDirectory}'.", path);

        using var stream = File.OpenRead(path);
        return Task.FromResult(SeedJsonReader.Deserialize<List<T>>(stream) ?? []);
    }
}
