using System.Globalization;
using System.Text;
using System.Text.Json;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Seeding;

namespace Tansekak.Infrastructure.Tests;

internal static class CutoffSeedSplitter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static void SplitMergedScienceFile(string seedDirectory)
    {
        var mergedPath = Path.Combine(seedDirectory, "cutoffs", "science-2026.md");
        var sciencePath = mergedPath;
        var mathematicsPath = Path.Combine(seedDirectory, "cutoffs", "mathematics-2026.md");
        var backupPath = Path.Combine(seedDirectory, "cutoffs", "science-2026-merged.backup.md");

        if (!File.Exists(mergedPath))
            throw new FileNotFoundException("Merged science cutoff file not found.", mergedPath);

        if (!File.Exists(backupPath))
            File.Copy(mergedPath, backupPath, overwrite: true);

        var catalog = LoadCatalog(seedDirectory);
        var facultyByUfId = catalog.ToDictionary(c => c.Entry.UniversityFacultyId, c => c.Faculty);
        var resolver = new CutoffNameResolver(catalog.Select(c => c.Entry));

        using var stream = File.OpenRead(backupPath);
        var (rows, parseErrors) = CutoffMarkdownParser.Parse(stream);
        if (parseErrors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", parseErrors.Select(e => e.Message)));

        var (resolved, unresolved) = resolver.Resolve(rows);
        if (unresolved.Count > 0)
            throw new InvalidOperationException(string.Join("; ", unresolved.Select(e => e.Message)));

        var scienceLines = new List<(string Label, decimal Score)>();
        var mathematicsLines = new List<(string Label, decimal Score)>();

        foreach (var row in resolved)
        {
            if (!facultyByUfId.TryGetValue(row.UniversityFacultyId, out var faculty))
                continue;

            if (faculty.AllowedTracks.Contains(AcademicTrack.Science))
                scienceLines.Add((row.SourceLabel, row.CutoffScore));

            if (faculty.AllowedTracks.Contains(AcademicTrack.Mathematics))
                mathematicsLines.Add((row.SourceLabel, row.CutoffScore));
        }

        WriteMarkdown(sciencePath, scienceLines);
        WriteMarkdown(mathematicsPath, mathematicsLines);
    }

    private static void WriteMarkdown(string path, List<(string Label, decimal Score)> lines)
    {
        var ordered = lines
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Label, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("| الكلية | الحد الأدنى |");
        sb.AppendLine("| --- | --- |");
        foreach (var (label, score) in ordered)
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {label} | {score:0.0} |");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static List<(CutoffCatalogEntry Entry, Faculty Faculty)> LoadCatalog(string seedDirectory)
    {
        var faculties = JsonSerializer.Deserialize<List<SeedFaculty>>(
            File.ReadAllText(Path.Combine(seedDirectory, "Faculties.json")), JsonOptions)!;
        var universities = JsonSerializer.Deserialize<List<SeedUniversity>>(
            File.ReadAllText(Path.Combine(seedDirectory, "Universities.json")), JsonOptions)!;
        var links = JsonSerializer.Deserialize<List<SeedUniversityFaculty>>(
            File.ReadAllText(Path.Combine(seedDirectory, "UniversityFaculties.json")), JsonOptions)!;

        var facultyById = faculties.ToDictionary(f => f.Id);
        var universityById = universities.ToDictionary(u => u.Id);

        return links
            .Where(l => facultyById.ContainsKey(l.FacultyId) && universityById.ContainsKey(l.UniversityId))
            .Select(l =>
            {
                var faculty = FacultySeedMapper.MapFaculty(facultyById[l.FacultyId]);
                var entry = new CutoffCatalogEntry(l.Id, universityById[l.UniversityId].NameAr, faculty.NameAr);
                return (entry, faculty);
            })
            .ToList();
    }
}
