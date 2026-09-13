using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Identity;

namespace Tansekak.Api.Controllers;

[ApiController]
[Route("api/admin/admission-years")]
[Authorize(Roles = Roles.Administrator)]
public class AdmissionYearsController(IAdmissionYearService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdmissionYearDto>>>> GetAll(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<AdmissionYearDto>>.Ok(await service.GetAllAsync(ct)));

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<AdmissionYearDto>>> GetCurrent(CancellationToken ct)
    {
        var item = await service.GetCurrentAsync(ct);
        return item is null ? ControllerResponses.NotFoundResponse<AdmissionYearDto>() : Ok(ApiResponse<AdmissionYearDto>.Ok(item));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdmissionYearDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? ControllerResponses.NotFoundResponse<AdmissionYearDto>() : Ok(ApiResponse<AdmissionYearDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdmissionYearDto>>> Create([FromBody] CreateAdmissionYearDto dto, CancellationToken ct) =>
        Ok(ApiResponse<AdmissionYearDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdmissionYearDto>>> Update(int id, [FromBody] UpdateAdmissionYearDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? ControllerResponses.NotFoundResponse<AdmissionYearDto>() : Ok(ApiResponse<AdmissionYearDto>.Ok(item));
    }

    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<ApiResponse<object>>> Publish(int id, CancellationToken ct) =>
        await service.PublishAsync(id, ct)
            ? Ok(ApiResponse<object>.Ok(new { }))
            : ControllerResponses.NotFoundResponse<object>();
}
