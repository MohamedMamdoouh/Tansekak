using System.Text.Json;

namespace Tansekak.Infrastructure.Seeding;

public static class SeedJsonReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options);

    public static T? Deserialize<T>(Stream stream) =>
        JsonSerializer.Deserialize<T>(stream, Options);
}
