using Tansekak.Application.DTOs;

namespace Tansekak.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IImportService
{
    Task<ImportResultDto> ImportAsync(int yearId, string track, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
