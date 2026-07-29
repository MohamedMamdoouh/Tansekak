using System.Text.Json;
using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Seeding;

public static class CutoffSeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<IReadOnlyList<AdmissionCutoff>> LoadCutoffsAsync(
        string dataPath,
        int admissionYearId,
        CancellationToken cancellationToken = default)
    {
        var filePath = ResolveCutoffsFile(dataPath);
        var rows = await ReadJsonAsync<SeedCutoff>(filePath, cancellationToken);
        return rows
            .Where(x => x.AdmissionYearId == admissionYearId)
            .Select(x => new AdmissionCutoff
            {
                Id = x.Id,
                AdmissionYearId = x.AdmissionYearId,
                UniversityFacultyId = x.UniversityFacultyId,
                Track = TrackParse(x.Track),
                CutoffScore = x.CutoffScore
            })
            .ToList();
    }

    public static string ResolveCutoffsFile(string dataPath)
    {
        var defaultFile = Path.Combine(dataPath, "AdmissionCutoffs2025.json");
        if (File.Exists(defaultFile))
            return defaultFile;

        var match = Directory.GetFiles(dataPath, "AdmissionCutoffs*.json")
            .OrderByDescending(Path.GetFileName)
            .FirstOrDefault();

        if (match is null)
            throw new FileNotFoundException("No admission cutoffs seed file found.", dataPath);

        return match;
    }

    public static AcademicTrack TrackParse(string track)
    {
        if (Enum.TryParse<AcademicTrack>(track, true, out var parsed))
            return parsed;

        throw new InvalidOperationException($"Invalid track value in seed data: \"{track}\".");
    }

    private static async Task<List<T>> ReadJsonAsync<T>(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var result = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, ct);
        return result ?? [];
    }

    private record SeedCutoff(int Id, int AdmissionYearId, int UniversityFacultyId, string Track, decimal CutoffScore);
}
