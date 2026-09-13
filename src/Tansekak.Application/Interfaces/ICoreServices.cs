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

public interface IStudentResultService
{
    Task<StudentResultDto?> GetBySeatingNoAsync(string seatingNo, CancellationToken cancellationToken = default);
}
