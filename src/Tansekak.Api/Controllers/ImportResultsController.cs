using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;

namespace Tansekak.Api.Controllers;

[ApiController]
[Route("api/admin/admission-years/{yearId:int}/import-results")]
[Authorize(Roles = Roles.Administrator)]
public class ImportResultsController(
    IStudentResultImportService importService,
    IR2Storage r2Storage,
    IImportJobService importJobService) : ControllerBase
{
    private const long DirectUploadLimitBytes = 20_971_520;

    [HttpPost]
    [RequestSizeLimit(DirectUploadLimitBytes)]
    public async Task<ActionResult<ApiResponse<ImportResultDto>>> Import(
        int yearId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<ImportResultDto>.Fail("File is required."));

        if (file.Length > DirectUploadLimitBytes)
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                ApiResponse<ImportResultDto>.Fail("File exceeds the 20 MB direct upload limit. Use the R2 upload flow."));

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<ImportResultDto>.Fail("Only .xlsx files are supported."));

        await using var stream = file.OpenReadStream();
        var result = await importService.ImportAsync(yearId, stream, file.FileName, ct);
        return ToActionResult(result);
    }

    [HttpPost("upload-url")]
    public async Task<ActionResult<ApiResponse<UploadUrlDto>>> CreateUploadUrl(
        int yearId,
        [FromBody] UploadUrlRequest request,
        CancellationToken ct)
    {
        if (!r2Storage.IsConfigured)
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<UploadUrlDto>.Fail("Large file uploads are unavailable. Configure Cloudflare R2."));

        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(ApiResponse<UploadUrlDto>.Fail("File name is required."));

        var (uploadUrl, objectKey) = await r2Storage.CreatePresignedUploadAsync(yearId, request.FileName, ct);
        return Ok(ApiResponse<UploadUrlDto>.Ok(new UploadUrlDto(uploadUrl, objectKey)));
    }

    [HttpPost("from-storage")]
    public async Task<ActionResult<ApiResponse<StartImportJobDto>>> ImportFromStorage(
        int yearId,
        [FromBody] FromStorageRequestDto request,
        CancellationToken ct)
    {
        if (!r2Storage.IsConfigured)
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<StartImportJobDto>.Fail("Large file uploads are unavailable. Configure Cloudflare R2."));

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
            return BadRequest(ApiResponse<StartImportJobDto>.Fail("Object key is required."));

        var job = await importJobService.CreateQueuedJobAsync(yearId, request.ObjectKey, ct);
        return Ok(ApiResponse<StartImportJobDto>.Ok(new StartImportJobDto(job.Id), "Import started."));
    }

    private ActionResult<ApiResponse<ImportResultDto>> ToActionResult(ImportResultDto result) =>
        result.Success
            ? Ok(ApiResponse<ImportResultDto>.Ok(result, result.Message))
            : BadRequest(ApiResponse<ImportResultDto>.Fail(result.Message, result.Errors?.Select(e => new ApiError
            {
                Field = e.Column,
                Message = e.Message,
                RowNumber = e.RowNumber,
                ErrorCode = e.ErrorCode
            }).ToList()));
}

public record UploadUrlRequest(string FileName);
