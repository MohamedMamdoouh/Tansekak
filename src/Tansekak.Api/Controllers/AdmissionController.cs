using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Identity;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Api.Controllers;

[ApiController]
[Route("api/admission")]
public class AdmissionController(IAdmissionPredictionService predictionService) : ControllerBase
{
    [HttpPost("predict")]
    public async Task<ActionResult<ApiResponse<PredictResponseDto>>> Predict(
        [FromBody] PredictRequestDto request, CancellationToken cancellationToken)
    {
        var result = await predictionService.PredictAsync(request, cancellationToken);
        return Ok(ApiResponse<PredictResponseDto>.Ok(result));
    }
}