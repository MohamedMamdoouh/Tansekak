using Microsoft.Extensions.Hosting;

namespace Tansekak.Infrastructure.Seeding;

public static class SeedDataPathResolver
{
    public static string? Resolve(IHostEnvironment env)
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "SeededData")),
            Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "Data")),
            Path.Combine(env.ContentRootPath, "Data"),
        };

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "Universities.json")))
                return candidate;
        }

        return null;
    }
}
