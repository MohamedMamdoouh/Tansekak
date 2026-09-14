namespace Tansekak.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public T? Data { get; set; }
    public List<ApiError>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully.") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string errorCode, List<ApiError>? errors = null) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            Message = ArabicErrorCatalog.GetMessage(errorCode),
            Errors = errors,
        };

    public static ApiResponse<T> Fail(string errorCode, string message, List<ApiError>? errors = null) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            Errors = errors,
        };
}

public class ApiError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? RowNumber { get; set; }
    public string? ErrorCode { get; set; }
}
