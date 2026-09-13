namespace Tansekak.Application.DTOs;

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record AdmissionYearDto(int Id, int Year, decimal MaximumScore, bool IsCurrent);
public record CreateAdmissionYearDto(int Year, decimal MaximumScore);
public record UpdateAdmissionYearDto(int Year, decimal MaximumScore);

public record AdmissionCutoffDto(
    int Id,
    int AdmissionYearId,
    int UniversityFacultyId,
    string Track,
    decimal CutoffScore,
    string? UniversityName = null,
    string? FacultyName = null);
public record CreateAdmissionCutoffDto(int AdmissionYearId, int UniversityFacultyId, string Track, decimal CutoffScore);
public record UpdateAdmissionCutoffDto(int AdmissionYearId, int UniversityFacultyId, string Track, decimal CutoffScore);
