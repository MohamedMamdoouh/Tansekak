namespace Tansekak.Infrastructure.Seeding;

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
