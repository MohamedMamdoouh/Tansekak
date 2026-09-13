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
[Route("api/admin/faculties")]
[Authorize(Roles = Roles.Administrator)]
public class FacultiesController(IFacultyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FacultyDto>>>> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<FacultyDto>>.Ok(await service.GetAllAsync(search, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<FacultyDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? ControllerResponses.NotFoundResponse<FacultyDto>() : Ok(ApiResponse<FacultyDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FacultyDto>>> Create([FromBody] CreateFacultyDto dto, CancellationToken ct) =>
        Ok(ApiResponse<FacultyDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<FacultyDto>>> Update(int id, [FromBody] UpdateFacultyDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? ControllerResponses.NotFoundResponse<FacultyDto>() : Ok(ApiResponse<FacultyDto>.Ok(item));
    }
}