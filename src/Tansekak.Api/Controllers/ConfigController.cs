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
[Route("api")]
public class ConfigController(IConfigService configService) : ControllerBase
{
    [HttpGet("config")]
    public async Task<ActionResult<ApiResponse<ConfigDto>>> Get(CancellationToken cancellationToken)
    {
        var config = await configService.GetConfigAsync(cancellationToken);
        return Ok(ApiResponse<ConfigDto>.Ok(config));
    }
}