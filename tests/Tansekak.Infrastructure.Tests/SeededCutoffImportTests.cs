using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Seeding;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class SeededCutoffImportTests
{
    [Fact]
    public async Task Science_and_literature_markdown_import_against_seed_catalog()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedCatalog(db);
            db.AdmissionYears.Add(new AdmissionYear
            {
                Id = 1,
                Year = 2026,
                MaximumScore = AdmissionDefaults.MaximumScore,
                IsCurrent = true
            });
            await db.SaveChangesAsync();

            var service = new ImportService(db, new EntityIdAllocator(db), NullLogger<ImportService>.Instance);

            var science = await ImportFileAsync(service, "science-2026.md", "Science");
            Assert.True(science.Success, FormatErrors("science-2026.md", science));
            Assert.True(science.ImportedCount >= 300, $"Expected many science cutoffs, got {science.ImportedCount}.");

            var literature = await ImportFileAsync(service, "literature-2026.md", "Literature");
            Assert.True(literature.Success, FormatErrors("literature-2026.md", literature));
            Assert.True(literature.ImportedCount >= 120, $"Expected many literature cutoffs, got {literature.ImportedCount}.");

            var cairoMedicine = await db.AdmissionCutoffs
                .Where(c => c.Track == AcademicTrack.Science && c.CutoffScore == 308.0m)
                .Join(db.UniversityFaculties, c => c.UniversityFacultyId, uf => uf.Id, (c, uf) => new { c, uf })
                .Join(db.Faculties, x => x.uf.FacultyId, f => f.Id, (x, f) => new { x.c, x.uf, f })
                .Join(db.Universities, x => x.uf.UniversityId, u => u.Id, (x, u) => new { x.f, u, x.c.CutoffScore })
                .FirstOrDefaultAsync(x => x.f.NameAr == "طب" && x.u.NameAr == "جامعة القاهرة");

            Assert.NotNull(cairoMedicine);
            Assert.Equal(308.0m, cairoMedicine!.CutoffScore);

            var vetAsMedicine = await db.AdmissionCutoffs
                .Where(c => c.Track == AcademicTrack.Science && c.CutoffScore == 279.0m)
                .Join(db.UniversityFaculties, c => c.UniversityFacultyId, uf => uf.Id, (c, uf) => uf)
                .Join(db.Faculties, uf => uf.FacultyId, f => f.Id, (_, f) => f.NameAr)
                .ToListAsync();
            Assert.DoesNotContain("طب", vetAsMedicine);

            Assert.Equal(science.ImportedCount, await db.AdmissionCutoffs.CountAsync(c => c.Track == AcademicTrack.Science));
            Assert.Equal(literature.ImportedCount, await db.AdmissionCutoffs.CountAsync(c => c.Track == AcademicTrack.Literature));
        }
    }

    [Theory]
    [InlineData("science-2026.md", "Science")]
    [InlineData("literature-2026.md", "Literature")]
    public void Seeded_markdown_parses_and_resolves_without_errors(string fileName, string trackName)
    {
        Assert.True(TrackHelper.TryParse(trackName, out var track));
        var catalog = LoadCatalog();
        var facultyByUfId = catalog.ToDictionary(c => c.Entry.UniversityFacultyId, c => c.Faculty);

        using var stream = File.OpenRead(CutoffPath(fileName));
        var (rows, parseErrors) = CutoffMarkdownParser.Parse(stream);
        Assert.Empty(parseErrors);
        Assert.NotEmpty(rows);

        var resolver = new CutoffNameResolver(catalog.Select(c => c.Entry));
        var (resolved, unresolved) = resolver.Resolve(rows);
        Assert.Empty(unresolved);
        Assert.Equal(rows.Count, resolved.Count);
        Assert.Equal(resolved.Count, resolved.Select(r => r.UniversityFacultyId).Distinct().Count());

        foreach (var row in resolved)
        {
            Assert.True(row.CutoffScore > 0);
            Assert.True(row.CutoffScore <= AdmissionDefaults.MaximumScore);
            Assert.True(facultyByUfId.TryGetValue(row.UniversityFacultyId, out var faculty));
            Assert.True(FacultyTrackValidator.IsTrackAllowed(faculty, track), row.SourceLabel);
        }
    }

    private static async Task<ImportResultDto> ImportFileAsync(
        ImportService service,
        string fileName,
        string track)
    {
        await using var stream = File.OpenRead(CutoffPath(fileName));
        return await service.ImportAsync(1, track, stream, fileName);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static void SeedCatalog(AppDbContext db)
    {
        var seedDir = SeededDataDirectory();
        var governorates = JsonSerializer.Deserialize<List<SeedGovernorate>>(
            File.ReadAllText(Path.Combine(seedDir, "Governorates.json")), JsonOptions)!;
        var faculties = JsonSerializer.Deserialize<List<SeedFaculty>>(
            File.ReadAllText(Path.Combine(seedDir, "Faculties.json")), JsonOptions)!;
        var universities = JsonSerializer.Deserialize<List<SeedUniversity>>(
            File.ReadAllText(Path.Combine(seedDir, "Universities.json")), JsonOptions)!;
        var links = JsonSerializer.Deserialize<List<SeedUniversityFaculty>>(
            File.ReadAllText(Path.Combine(seedDir, "UniversityFaculties.json")), JsonOptions)!;

        var governorateIds = governorates.Select(g => g.Id).ToHashSet();
        var facultyIds = faculties.Select(f => f.Id).ToHashSet();
        var universityIds = universities.Select(u => u.Id).ToHashSet();

        db.Governorates.AddRange(governorates.Select(g => new Governorate { Id = g.Id, NameAr = g.NameAr }));
        db.Faculties.AddRange(faculties.Select(FacultySeedMapper.MapFaculty));
        db.Universities.AddRange(universities
            .Where(u => governorateIds.Contains(u.GovernorateId))
            .Select(u => new University
            {
                Id = u.Id,
                NameAr = u.NameAr,
                GovernorateId = u.GovernorateId,
                Type = Enum.TryParse<UniversityType>(u.Type, true, out var t) ? t : UniversityType.Public
            }));
        db.UniversityFaculties.AddRange(links
            .Where(l => universityIds.Contains(l.UniversityId) && facultyIds.Contains(l.FacultyId))
            .Select(l => new UniversityFaculty
            {
                Id = l.Id,
                UniversityId = l.UniversityId,
                FacultyId = l.FacultyId
            }));
        db.SaveChanges();
    }

    private static List<(CutoffCatalogEntry Entry, Faculty Faculty)> LoadCatalog()
    {
        var seedDir = SeededDataDirectory();
        var faculties = JsonSerializer.Deserialize<List<SeedFaculty>>(
            File.ReadAllText(Path.Combine(seedDir, "Faculties.json")), JsonOptions)!;
        var universities = JsonSerializer.Deserialize<List<SeedUniversity>>(
            File.ReadAllText(Path.Combine(seedDir, "Universities.json")), JsonOptions)!;
        var links = JsonSerializer.Deserialize<List<SeedUniversityFaculty>>(
            File.ReadAllText(Path.Combine(seedDir, "UniversityFaculties.json")), JsonOptions)!;

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

    private static string CutoffPath(string fileName) =>
        Path.Combine(SeededDataDirectory(), "cutoffs", fileName);

    private static string SeededDataDirectory()
    {
        var fromOutput = Path.Combine(AppContext.BaseDirectory, "SeededData");
        if (Directory.Exists(fromOutput))
            return fromOutput;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "SeededData");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("SeededData directory was not found.");
    }

    private static string FormatErrors(string fileName, Application.DTOs.ImportResultDto result)
    {
        var errors = result.Errors?.Select(e => $"{e.RowNumber}:{e.Column}:{e.ErrorCode}:{e.Message}") ?? [];
        return $"{fileName} import failed: {result.Message}. {string.Join(" | ", errors)}";
    }
}
