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
[Route("api/thanaweya-results")]
public class ThanaweyaResultsController(IStudentResultService service) : ControllerBase
{
    [HttpGet("{seatingNo}")]
    public async Task<ActionResult<ApiResponse<StudentResultDto>>> Get(string seatingNo, CancellationToken ct)
    {
        var result = await service.GetBySeatingNoAsync(seatingNo, ct);
        return result is null
            ? NotFound(ApiResponse<StudentResultDto>.Fail("لم يتم العثور على نتيجة لهذا الرقم."))
            : Ok(ApiResponse<StudentResultDto>.Ok(result));
    }
}