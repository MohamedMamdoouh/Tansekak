using System.Net;
using System.Text.Json;
using Tansekak.Application.Common;

namespace Tansekak.Api.Middleware;

public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "Application error {ErrorCode}", ex.ErrorCode);
            await WriteError(context, ex.StatusCode, ex.ErrorCode, ex.UserMessage);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error");
            await WriteError(context, HttpStatusCode.BadRequest, ApiErrorCodes.ValidationFailed);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business rule violation");
            await WriteError(context, HttpStatusCode.BadRequest, ApiErrorCodes.ValidationFailed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error");
            await WriteError(context, HttpStatusCode.InternalServerError, ApiErrorCodes.InternalError);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static async Task WriteError(
        HttpContext context,
        HttpStatusCode code,
        string errorCode,
        string? message = null)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException(ArabicErrorCatalog.GetMessage(ApiErrorCodes.InternalError));

        context.Response.StatusCode = (int)code;
        context.Response.ContentType = "application/json";
        var response = ApiResponse<object>.Fail(errorCode, message ?? ArabicErrorCatalog.GetMessage(errorCode));
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
