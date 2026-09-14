using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Identity;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Api.Controllers;

[ApiController]
[Route("api/admin/admission-years/{yearId:int}/import")]
[Authorize(Roles = Roles.Administrator)]
public class ImportController(IImportService importService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ApiResponse<ImportResultDto>>> Import(int yearId, IFormFile file, [FromForm] string track, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<ImportResultDto>.Fail(ApiErrorCodes.FileRequired));

        if (string.IsNullOrWhiteSpace(track))
            return BadRequest(ApiResponse<ImportResultDto>.Fail(ApiErrorCodes.TrackRequired));

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".md", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<ImportResultDto>.Fail(ApiErrorCodes.OnlyMdFiles));

        await using var stream = file.OpenReadStream();
        var result = await importService.ImportAsync(yearId, track, stream, file.FileName, ct);
        return result.Success
            ? Ok(ApiResponse<ImportResultDto>.Ok(result, result.Message))
            : BadRequest(ApiResponse<ImportResultDto>.Fail(
                ApiErrorCodes.ValidationFailed,
                result.Message,
                result.Errors?.Select(e => new ApiError
                {
                    Field = e.Column,
                    Message = e.Message,
                    RowNumber = e.RowNumber,
                    ErrorCode = e.ErrorCode
                }).ToList()));
    }
}
