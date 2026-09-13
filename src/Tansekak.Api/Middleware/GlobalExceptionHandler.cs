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
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            await WriteError(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ServiceUnavailableException ex)
        {
            logger.LogWarning(ex, "Service unavailable");
            await WriteError(context, HttpStatusCode.ServiceUnavailable, ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error");
            await WriteError(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business rule violation");
            await WriteError(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error");
            await WriteError(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static async Task WriteError(HttpContext context, HttpStatusCode code, string message)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException(message);

        context.Response.StatusCode = (int)code;
        context.Response.ContentType = "application/json";
        var response = ApiResponse<object>.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
