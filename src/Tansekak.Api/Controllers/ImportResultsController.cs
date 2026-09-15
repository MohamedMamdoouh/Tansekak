using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Identity;
using Tansekak.Infrastructure.Services;

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
    private const long StagedUploadLimitBytes = 104_857_600;
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpPost]
    [RequestSizeLimit(DirectUploadLimitBytes)]
    public async Task<ActionResult<ApiResponse<ImportResultDto>>> Import(
        int yearId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<ImportResultDto>.Fail(ApiErrorCodes.FileRequired));

        if (file.Length > DirectUploadLimitBytes)
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                ApiResponse<ImportResultDto>.Fail(ApiErrorCodes.FileTooLarge));

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<ImportResultDto>.Fail(ApiErrorCodes.OnlyXlsxFiles));

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
                ApiResponse<UploadUrlDto>.Fail(ApiErrorCodes.R2NotConfigured));

        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(ApiResponse<UploadUrlDto>.Fail(ApiErrorCodes.FileNameRequired));

        var (uploadUrl, objectKey) = await r2Storage.CreatePresignedUploadAsync(yearId, request.FileName, ct);
        return Ok(ApiResponse<UploadUrlDto>.Ok(new UploadUrlDto(uploadUrl, objectKey)));
    }

    [HttpPost("from-upload")]
    [RequestSizeLimit(StagedUploadLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = StagedUploadLimitBytes)]
    public async Task<ActionResult<ApiResponse<StartImportJobDto>>> ImportFromUpload(
        int yearId,
        IFormFile file,
        CancellationToken ct)
    {
        if (!r2Storage.IsConfigured)
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<StartImportJobDto>.Fail(ApiErrorCodes.R2NotConfigured));

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<StartImportJobDto>.Fail(ApiErrorCodes.FileRequired));

        if (file.Length > StagedUploadLimitBytes)
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                ApiResponse<StartImportJobDto>.Fail(
                    ApiErrorCodes.FileTooLarge,
                    "حجم الملف يتجاوز 100 ميجابايت."));

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<StartImportJobDto>.Fail(ApiErrorCodes.OnlyXlsxFiles));

        var objectKey = $"imports/{yearId}/{Guid.NewGuid():N}{ext}";
        await using var stream = file.OpenReadStream();
        await r2Storage.UploadAsync(objectKey, stream, ExcelContentType, ct);

        // Create/enqueue without the request CT so a disconnect after R2 upload cannot
        // leave a Queued row that only runs on the next restart. If the client already
        // aborted, cancel immediately so the background worker never replaces results.
        var job = await importJobService.CreateQueuedJobAsync(yearId, objectKey, CancellationToken.None);
        if (ct.IsCancellationRequested)
        {
            await importJobService.CancelAsync(job.Id, CancellationToken.None);
            return StatusCode(
                499,
                ApiResponse<StartImportJobDto>.Fail(ApiErrorCodes.ImportJobCancelled));
        }

        return Ok(ApiResponse<StartImportJobDto>.Ok(new StartImportJobDto(job.Id), "Import started."));
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
                ApiResponse<StartImportJobDto>.Fail(ApiErrorCodes.R2NotConfigured));

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
            return BadRequest(ApiResponse<StartImportJobDto>.Fail(ApiErrorCodes.ObjectKeyRequired));

        var job = await importJobService.CreateQueuedJobAsync(yearId, request.ObjectKey, CancellationToken.None);
        if (ct.IsCancellationRequested)
        {
            await importJobService.CancelAsync(job.Id, CancellationToken.None);
            return StatusCode(
                499,
                ApiResponse<StartImportJobDto>.Fail(ApiErrorCodes.ImportJobCancelled));
        }

        return Ok(ApiResponse<StartImportJobDto>.Ok(new StartImportJobDto(job.Id), "Import started."));
    }

    private ActionResult<ApiResponse<ImportResultDto>> ToActionResult(ImportResultDto result) =>
        result.Success
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

public record UploadUrlRequest(string FileName);
