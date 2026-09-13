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
[Route("api/admin/universities")]
[Authorize(Roles = Roles.Administrator)]
public class UniversitiesController(IUniversityService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UniversityDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] int? governorateId, [FromQuery] string? type, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<UniversityDto>>.Ok(await service.GetAllAsync(search, governorateId, type, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UniversityDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? ControllerResponses.NotFoundResponse<UniversityDto>() : Ok(ApiResponse<UniversityDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UniversityDto>>> Create([FromBody] CreateUniversityDto dto, CancellationToken ct) =>
        Ok(ApiResponse<UniversityDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UniversityDto>>> Update(int id, [FromBody] UpdateUniversityDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? ControllerResponses.NotFoundResponse<UniversityDto>() : Ok(ApiResponse<UniversityDto>.Ok(item));
    }
}