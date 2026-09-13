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
[Route("api/admin/dashboard")]
[Authorize(Roles = Roles.Administrator)]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse<DashboardDto>.Ok(await dashboardService.GetDashboardAsync(cancellationToken)));
}