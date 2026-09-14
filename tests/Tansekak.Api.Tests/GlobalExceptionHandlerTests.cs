using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Tansekak.Api.Middleware;
using Tansekak.Application.Common;

namespace Tansekak.Api.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task UnknownExceptionReturnsInternalErrorWithoutInternalDetails()
    {
        var context = CreateContext();
        var handler = new GlobalExceptionHandler(_ => throw new Exception("SQL connection failed at server/db"), NullLogger<GlobalExceptionHandler>.Instance);

        await handler.InvokeAsync(context);

        var body = await ReadBody(context);
        using var json = JsonDocument.Parse(body);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal(ApiErrorCodes.InternalError, json.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(ArabicErrorCatalog.GetMessage(ApiErrorCodes.InternalError), json.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain("SQL", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NotFoundExceptionReturnsArabicMessageAndCode()
    {
        var context = CreateContext();
        var handler = new GlobalExceptionHandler(
            _ => throw new NotFoundException(ApiErrorCodes.StudentResultNotFound),
            NullLogger<GlobalExceptionHandler>.Instance);

        await handler.InvokeAsync(context);

        var body = await ReadBody(context);
        using var json = JsonDocument.Parse(body);

        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        Assert.Equal(ApiErrorCodes.StudentResultNotFound, json.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(
            ArabicErrorCatalog.GetMessage(ApiErrorCodes.StudentResultNotFound),
            json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ArgumentExceptionReturnsValidationFailedWithoutRawMessage()
    {
        var context = CreateContext();
        var handler = new GlobalExceptionHandler(
            _ => throw new ArgumentException("Invalid track."),
            NullLogger<GlobalExceptionHandler>.Instance);

        await handler.InvokeAsync(context);

        var body = await ReadBody(context);
        using var json = JsonDocument.Parse(body);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal(ApiErrorCodes.ValidationFailed, json.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(ArabicErrorCatalog.GetMessage(ApiErrorCodes.ValidationFailed), json.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain("Invalid track", body, StringComparison.OrdinalIgnoreCase);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
