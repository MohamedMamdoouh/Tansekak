using Tansekak.Application.DTOs;

namespace Tansekak.Application.Interfaces;

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
