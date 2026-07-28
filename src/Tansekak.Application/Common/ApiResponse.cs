namespace Tansekak.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<ApiError>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully.") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<ApiError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

public class ApiError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? RowNumber { get; set; }
    public string? ErrorCode { get; set; }
}
