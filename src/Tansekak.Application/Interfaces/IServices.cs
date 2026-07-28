using Tansekak.Application.DTOs;

namespace Tansekak.Application.Interfaces;

public interface IConfigService
{
    Task<ConfigDto> GetConfigAsync(CancellationToken cancellationToken = default);
}

public interface IAdmissionPredictionService
{
    Task<PredictResponseDto> PredictAsync(PredictRequestDto request, CancellationToken cancellationToken = default);
}

public interface IGovernorateService
{
    Task<IReadOnlyList<GovernorateDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GovernorateDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GovernorateDto> CreateAsync(CreateGovernorateDto dto, CancellationToken cancellationToken = default);
    Task<GovernorateDto?> UpdateAsync(int id, UpdateGovernorateDto dto, CancellationToken cancellationToken = default);
}

public interface IUniversityService
{
    Task<IReadOnlyList<UniversityDto>> GetAllAsync(string? search = null, int? governorateId = null, string? type = null, CancellationToken cancellationToken = default);
    Task<UniversityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UniversityDto> CreateAsync(CreateUniversityDto dto, CancellationToken cancellationToken = default);
    Task<UniversityDto?> UpdateAsync(int id, UpdateUniversityDto dto, CancellationToken cancellationToken = default);
}

public interface IFacultyService
{
    Task<IReadOnlyList<FacultyDto>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<FacultyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FacultyDto> CreateAsync(CreateFacultyDto dto, CancellationToken cancellationToken = default);
    Task<FacultyDto?> UpdateAsync(int id, UpdateFacultyDto dto, CancellationToken cancellationToken = default);
}

public interface IUniversityFacultyService
{
    Task<IReadOnlyList<UniversityFacultyDto>> GetAllAsync(string? search = null, int? universityId = null, int? facultyId = null, CancellationToken cancellationToken = default);
    Task<UniversityFacultyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UniversityFacultyDto> CreateAsync(CreateUniversityFacultyDto dto, CancellationToken cancellationToken = default);
    Task<UniversityFacultyDto?> UpdateAsync(int id, UpdateUniversityFacultyDto dto, CancellationToken cancellationToken = default);
}

public interface IAdmissionYearService
{
    Task<IReadOnlyList<AdmissionYearDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AdmissionYearDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdmissionYearDto> CreateAsync(CreateAdmissionYearDto dto, CancellationToken cancellationToken = default);
    Task<AdmissionYearDto?> UpdateAsync(int id, UpdateAdmissionYearDto dto, CancellationToken cancellationToken = default);
    Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAdmissionCutoffService
{
    Task<PagedResultDto<AdmissionCutoffDto>> GetPagedAsync(int? yearId = null, string? search = null, string? track = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdmissionCutoffDto>> GetAllAsync(int? yearId = null, string? search = null, string? track = null, CancellationToken cancellationToken = default);
    Task<AdmissionCutoffDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdmissionCutoffDto> CreateAsync(CreateAdmissionCutoffDto dto, CancellationToken cancellationToken = default);
    Task<AdmissionCutoffDto?> UpdateAsync(int id, UpdateAdmissionCutoffDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IImportService
{
    Task<ImportResultDto> ImportAsync(int yearId, string track, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}

public interface IStudentResultImportService
{
    Task<ImportResultDto> ImportAsync(int yearId, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}

public interface IStudentResultService
{
    Task<StudentResultDto?> LookupBySeatingNoAsync(string seatingNo, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
