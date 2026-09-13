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
[Route("api/admin/governorates")]
[Authorize(Roles = Roles.Administrator)]
public class GovernoratesController(IGovernorateService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GovernorateDto>>>> GetAll(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<GovernorateDto>>.Ok(await service.GetAllAsync(ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GovernorateDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? ControllerResponses.NotFoundResponse<GovernorateDto>() : Ok(ApiResponse<GovernorateDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GovernorateDto>>> Create([FromBody] CreateGovernorateDto dto, CancellationToken ct) =>
        Ok(ApiResponse<GovernorateDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<GovernorateDto>>> Update(int id, [FromBody] UpdateGovernorateDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? ControllerResponses.NotFoundResponse<GovernorateDto>() : Ok(ApiResponse<GovernorateDto>.Ok(item));
    }
}