namespace Tansekak.Application.DTOs;

public record ImportValidationErrorDto(int RowNumber, string Column, string ErrorCode, string Message);
public record ImportResultDto(bool Success, string Message, int? ImportedCount = null, IReadOnlyList<ImportValidationErrorDto>? Errors = null);

public record ImportJobDto(
    Guid Id,
    string Status,
    int? ImportedCount,
    string? Message,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record UploadUrlDto(string UploadUrl, string ObjectKey);

public record FromStorageRequestDto(string ObjectKey);

public record StartImportJobDto(Guid JobId);
