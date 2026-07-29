namespace Tansekak.Application.DTOs;

public record ConfigDto(
    string AppName,
    int CurrentYear,
    decimal MaximumScore,
    IReadOnlyList<string> Tracks);

public record PredictRequestDto(string Track, decimal Score, int Page = 1, int PageSize = 10);

public record NamedEntityDto(string NameAr);

public record AdmissionResultDto(
    NamedEntityDto University,
    NamedEntityDto Faculty);

public record PredictResponseDto(
    IReadOnlyList<AdmissionResultDto> Results,
    bool HasMore,
    int TotalCount);

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record GovernorateDto(int Id, string NameAr);
public record CreateGovernorateDto(string NameAr);
public record UpdateGovernorateDto(string NameAr);

public record UniversityDto(int Id, string NameAr, int GovernorateId, string Type, string? GovernorateName = null);
public record CreateUniversityDto(string NameAr, int GovernorateId, string Type);
public record UpdateUniversityDto(string NameAr, int GovernorateId, string Type);

public record FacultyDto(int Id, string NameAr);
public record CreateFacultyDto(string NameAr);
public record UpdateFacultyDto(string NameAr);

public record UniversityFacultyDto(
    int Id,
    int UniversityId,
    int FacultyId,
    string? UniversityName = null,
    string? FacultyName = null);
public record CreateUniversityFacultyDto(int UniversityId, int FacultyId);
public record UpdateUniversityFacultyDto(int UniversityId, int FacultyId);

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

public record LoginRequestDto(string Email, string Password);
public record AuthUserDto(string Email, string Role);

public record DashboardDto(
    int GovernoratesCount,
    int UniversitiesCount,
    int FacultiesCount,
    int UniversityFacultiesCount,
    int CutoffsCount,
    int StudentResultsCount,
    int? CurrentYear);

public record ImportValidationErrorDto(int RowNumber, string Column, string ErrorCode, string Message);
public record ImportResultDto(bool Success, string Message, int? ImportedCount = null, IReadOnlyList<ImportValidationErrorDto>? Errors = null);

public record ImportJobStartedDto(Guid JobId, string Status);
public record ImportJobStatusDto(Guid JobId, string Status, ImportResultDto? Result, string? Message);

public record CreateImportUploadUrlDto(string FileName, long TotalSize);
public record ImportUploadUrlDto(string UploadUrl, string ObjectKey, int ExpiresInSeconds);
public record ImportFromStorageDto(string ObjectKey, string FileName);

public record StudentResultDto(
    string SeatingNo,
    string ArabicName,
    decimal TotalDegree,
    string StudentCaseDesc,
    int Year);
