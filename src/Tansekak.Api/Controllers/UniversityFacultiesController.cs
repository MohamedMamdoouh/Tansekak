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
[Route("api/admin/university-faculties")]
[Authorize(Roles = Roles.Administrator)]
public class UniversityFacultiesController(IUniversityFacultyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UniversityFacultyDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] int? universityId, [FromQuery] int? facultyId, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<UniversityFacultyDto>>.Ok(await service.GetAllAsync(search, universityId, facultyId, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UniversityFacultyDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? ControllerResponses.NotFoundResponse<UniversityFacultyDto>() : Ok(ApiResponse<UniversityFacultyDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UniversityFacultyDto>>> Create([FromBody] CreateUniversityFacultyDto dto, CancellationToken ct) =>
        Ok(ApiResponse<UniversityFacultyDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UniversityFacultyDto>>> Update(int id, [FromBody] UpdateUniversityFacultyDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? ControllerResponses.NotFoundResponse<UniversityFacultyDto>() : Ok(ApiResponse<UniversityFacultyDto>.Ok(item));
    }
}