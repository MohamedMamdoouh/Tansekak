namespace Tansekak.Application.DTOs;

public record ImportValidationErrorDto(int RowNumber, string Column, string ErrorCode, string Message);
public record ImportResultDto(bool Success, string Message, int? ImportedCount = null, IReadOnlyList<ImportValidationErrorDto>? Errors = null);
