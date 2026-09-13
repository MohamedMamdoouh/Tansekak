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
[Route("api/admin/admission-cutoffs")]
[Authorize(Roles = Roles.Administrator)]
public class AdmissionCutoffsController(IAdmissionCutoffService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<AdmissionCutoffDto>>>> GetAll(
        [FromQuery] int? yearId, [FromQuery] string? search, [FromQuery] string? track,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        Ok(ApiResponse<PagedResultDto<AdmissionCutoffDto>>.Ok(
            await service.GetPagedAsync(yearId, search, track, page, pageSize, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdmissionCutoffDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? ControllerResponses.NotFoundResponse<AdmissionCutoffDto>() : Ok(ApiResponse<AdmissionCutoffDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdmissionCutoffDto>>> Create([FromBody] CreateAdmissionCutoffDto dto, CancellationToken ct) =>
        Ok(ApiResponse<AdmissionCutoffDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdmissionCutoffDto>>> Update(int id, [FromBody] UpdateAdmissionCutoffDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? ControllerResponses.NotFoundResponse<AdmissionCutoffDto>() : Ok(ApiResponse<AdmissionCutoffDto>.Ok(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await service.DeleteAsync(id, ct);
        return ok ? Ok(ApiResponse<object>.Ok(new { }, "Deleted successfully.")) : ControllerResponses.NotFoundResult();
    }
}