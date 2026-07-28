using Tansekak.Application.DTOs;

namespace Tansekak.Infrastructure.Import;

public sealed class CutoffNameResolver
{
    private readonly Dictionary<string, CutoffCatalogEntry> _exactAliases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CutoffCatalogEntry> _overrideAliases = new(StringComparer.Ordinal);
    private readonly List<(string SuffixKey, CutoffCatalogEntry Entry)> _suffixEntries = [];

    public CutoffNameResolver(
        IEnumerable<CutoffCatalogEntry> catalog,
        IEnumerable<CutoffNameOverride>? overrides = null)
    {
        foreach (var entry in catalog)
        {
            foreach (var alias in BuildAliases(entry))
                AddExactAlias(alias, entry);

            var suffix = ArabicTextNormalizer.NormalizeKey(
                ArabicTextNormalizer.UniversityShortName(entry.UniversityNameAr));
            if (!string.IsNullOrEmpty(suffix))
                _suffixEntries.Add((suffix, entry));
        }

        _suffixEntries.Sort((a, b) => b.SuffixKey.Length.CompareTo(a.SuffixKey.Length));

        foreach (var item in overrides ?? [])
        {
            var entry = catalog.FirstOrDefault(c =>
                string.Equals(c.UniversityNameAr, item.UniversityNameAr, StringComparison.OrdinalIgnoreCase)
                && string.Equals(c.FacultyNameAr, item.FacultyNameAr, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
                continue;

            _overrideAliases[ArabicTextNormalizer.Normalize(item.SourceLabel)] = entry;
            _overrideAliases[ArabicTextNormalizer.NormalizeKey(item.SourceLabel)] = entry;
        }
    }

    public (List<ResolvedCutoffRow> Resolved, List<ImportValidationErrorDto> Unresolved) Resolve(
        IEnumerable<ParsedCutoffRow> rows)
    {
        var resolved = new List<ResolvedCutoffRow>();
        var unresolved = new List<ImportValidationErrorDto>();

        foreach (var row in rows)
        {
            if (TryResolve(row.SourceLabel, out var entry))
            {
                resolved.Add(new ResolvedCutoffRow(
                    row.LineNumber,
                    row.SourceLabel,
                    entry.UniversityFacultyId,
                    entry.UniversityNameAr,
                    entry.FacultyNameAr,
                    row.CutoffScore));
            }
            else
            {
                unresolved.Add(new ImportValidationErrorDto(
                    row.LineNumber,
                    "الكلية",
                    "UNRESOLVED",
                    $"Could not match \"{row.SourceLabel}\" to a university/faculty pair."));
            }
        }

        return (resolved, unresolved);
    }

    private bool TryResolve(string sourceLabel, out CutoffCatalogEntry entry)
    {
        entry = null!;
        var normalized = ArabicTextNormalizer.Normalize(sourceLabel);
        var normalizedKey = ArabicTextNormalizer.NormalizeKey(sourceLabel);

        if (_overrideAliases.TryGetValue(normalized, out var overrideMatch)
            || _overrideAliases.TryGetValue(normalizedKey, out overrideMatch))
        {
            entry = overrideMatch;
            return true;
        }

        if (_exactAliases.TryGetValue(normalized, out var exactMatch)
            || _exactAliases.TryGetValue(normalizedKey, out exactMatch))
        {
            entry = exactMatch;
            return true;
        }

        foreach (var (suffixKey, candidate) in _suffixEntries)
        {
            if (!normalizedKey.EndsWith(suffixKey, StringComparison.Ordinal))
                continue;

            var facultyPart = normalizedKey[..^suffixKey.Length];
            var facultyKey = ArabicTextNormalizer.NormalizeKey(candidate.FacultyNameAr);
            if (facultyPart.Equals(facultyKey, StringComparison.Ordinal)
                || facultyPart.Contains(facultyKey, StringComparison.Ordinal)
                || facultyKey.Contains(facultyPart, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> BuildAliases(CutoffCatalogEntry entry)
    {
        var faculty = entry.FacultyNameAr.Trim();
        var university = entry.UniversityNameAr.Trim();
        var shortName = ArabicTextNormalizer.UniversityShortName(university);

        foreach (var alias in BuildLabelVariants(faculty, shortName))
        {
            yield return ArabicTextNormalizer.Normalize(alias);
            yield return ArabicTextNormalizer.NormalizeKey(alias);
        }

        foreach (var alias in BuildLabelVariants(faculty, university))
        {
            yield return ArabicTextNormalizer.Normalize(alias);
            yield return ArabicTextNormalizer.NormalizeKey(alias);
        }

        var facultyWithoutAl = faculty.StartsWith("ال", StringComparison.Ordinal) ? faculty[2..] : faculty;
        foreach (var alias in BuildLabelVariants(facultyWithoutAl, shortName))
        {
            yield return ArabicTextNormalizer.Normalize(alias);
            yield return ArabicTextNormalizer.NormalizeKey(alias);
        }

        if (faculty.Contains('(') || faculty.Contains(')'))
        {
            var simplified = faculty.Replace("(", " ").Replace(")", " ").Trim();
            foreach (var alias in BuildLabelVariants(simplified, shortName))
            {
                yield return ArabicTextNormalizer.Normalize(alias);
                yield return ArabicTextNormalizer.NormalizeKey(alias);
            }
        }

        foreach (var alias in BuildMdStyleAliases(faculty, shortName))
        {
            yield return ArabicTextNormalizer.Normalize(alias);
            yield return ArabicTextNormalizer.NormalizeKey(alias);
        }
    }

    private static IEnumerable<string> BuildLabelVariants(string faculty, string universityPart)
    {
        yield return $"{faculty} {universityPart}";
    }

    private static IEnumerable<string> BuildMdStyleAliases(string faculty, string shortName)
    {
        var normalizedFaculty = ArabicTextNormalizer.Normalize(faculty);

        if (normalizedFaculty is "اسنان")
            yield return $"طب اسنان {shortName}";

        if (normalizedFaculty is "صيدله")
            yield return $"صيدله وتصنيع دوائي {shortName}";

        if (normalizedFaculty is "العلاج الطبيعي" or "علاج طبيعي")
            yield return $"علاج طبيعي {shortName}";

        if (normalizedFaculty is "الاقتصاد والعلوم السياسيه")
            yield return $"اقتصاد و علوم سياسيه {shortName}";

        if (normalizedFaculty is "الاعلام")
            yield return $"اعلام {shortName}";

        if (normalizedFaculty is "الاثار")
            yield return $"آثار {shortName}";
    }

    private void AddExactAlias(string alias, CutoffCatalogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        if (!_exactAliases.ContainsKey(alias))
            _exactAliases[alias] = entry;
    }
}
