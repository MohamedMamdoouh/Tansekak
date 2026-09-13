using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;

namespace Tansekak.Api.Controllers;

[ApiController]
[Route("api/admin/import-jobs")]
[Authorize(Roles = Roles.Administrator)]
public class ImportJobsController(IImportJobService importJobService) : ControllerBase
{
    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<ImportJobDto>>> Get(Guid jobId, CancellationToken ct)
    {
        var job = await importJobService.GetAsync(jobId, ct);
        return job is null
            ? ControllerResponses.NotFoundResponse<ImportJobDto>()
            : Ok(ApiResponse<ImportJobDto>.Ok(job));
    }
}
