using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;

namespace Tansekak.Api.Controllers;

public static class ControllerResponses
{
    public static ActionResult<ApiResponse<T>> NotFoundResponse<T>() =>
        new NotFoundObjectResult(ApiResponse<T>.Fail(ApiErrorCodes.NotFound));

    public static IActionResult NotFoundResult() =>
        new NotFoundObjectResult(ApiResponse<object>.Fail(ApiErrorCodes.NotFound));
}
