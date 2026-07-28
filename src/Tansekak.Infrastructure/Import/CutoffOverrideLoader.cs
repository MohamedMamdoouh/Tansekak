using System.Reflection;
using System.Text.Json;
using Tansekak.Infrastructure.Import;

namespace Tansekak.Infrastructure.Import;

public static class CutoffOverrideLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<CutoffNameOverride> Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Tansekak.Infrastructure.Data.cutoff-name-overrides.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "cutoff-name-overrides.json");
            if (!File.Exists(path))
                return [];

            return JsonSerializer.Deserialize<List<CutoffNameOverride>>(File.ReadAllText(path), JsonOptions) ?? [];
        }

        return JsonSerializer.Deserialize<List<CutoffNameOverride>>(stream, JsonOptions) ?? [];
    }
}
