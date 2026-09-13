using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;

namespace Tansekak.Api.Controllers;

public static class ControllerResponses
{
    public const string NotFoundMessage = "Not found.";

    public static ActionResult<ApiResponse<T>> NotFoundResponse<T>() =>
        new NotFoundObjectResult(ApiResponse<T>.Fail(NotFoundMessage));

    public static IActionResult NotFoundResult() =>
        new NotFoundObjectResult(ApiResponse<object>.Fail(NotFoundMessage));
}
