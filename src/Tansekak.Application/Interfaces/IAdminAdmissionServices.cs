using Tansekak.Application.DTOs;

namespace Tansekak.Application.Interfaces;

public interface IAdmissionYearService
{
    Task<IReadOnlyList<AdmissionYearDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AdmissionYearDto?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<AdmissionYearDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdmissionYearDto> CreateAsync(CreateAdmissionYearDto dto, CancellationToken cancellationToken = default);
    Task<AdmissionYearDto?> UpdateAsync(int id, UpdateAdmissionYearDto dto, CancellationToken cancellationToken = default);
    Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAdmissionCutoffService
{
    Task<PagedResultDto<AdmissionCutoffDto>> GetPagedAsync(int? yearId = null, string? search = null, string? track = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<AdmissionCutoffDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdmissionCutoffDto> CreateAsync(CreateAdmissionCutoffDto dto, CancellationToken cancellationToken = default);
    Task<AdmissionCutoffDto?> UpdateAsync(int id, UpdateAdmissionCutoffDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
