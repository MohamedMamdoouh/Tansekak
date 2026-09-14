using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Tansekak.Application.Common;

namespace Tansekak.Api.Middleware;

public sealed class AuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        if (authorizeResult.Challenged)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, ApiErrorCodes.NotAuthenticated);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await WriteError(context, StatusCodes.Status403Forbidden, ApiErrorCodes.Forbidden);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static async Task WriteError(HttpContext context, int statusCode, string errorCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var response = ApiResponse<object>.Fail(errorCode);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
