namespace Tansekak.Infrastructure.Import;

public sealed record ParsedCutoffRow(int LineNumber, string SourceLabel, decimal CutoffScore);

public sealed record CutoffCatalogEntry(
    int UniversityFacultyId,
    string UniversityNameAr,
    string FacultyNameAr);

public sealed record ResolvedCutoffRow(
    int LineNumber,
    string SourceLabel,
    int UniversityFacultyId,
    string UniversityNameAr,
    string FacultyNameAr,
    decimal CutoffScore);

public sealed record CutoffNameOverride(
    string SourceLabel,
    string UniversityNameAr,
    string FacultyNameAr);
