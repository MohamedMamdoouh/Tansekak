using Tansekak.Application.Common;

namespace Tansekak.Application.Tests;

public class ArabicErrorCatalogTests
{
    [Fact]
    public void AllDefinedCodesHaveArabicMessages()
    {
        foreach (var code in ApiErrorCodes.All)
        {
            var message = ArabicErrorCatalog.GetMessage(code);
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.DoesNotContain("Error", message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void UnknownCodeReturnsInternalErrorMessage()
    {
        var message = ArabicErrorCatalog.GetMessage("UNKNOWN_CODE");
        Assert.Equal(ArabicErrorCatalog.GetMessage(ApiErrorCodes.InternalError), message);
    }

    [Fact]
    public void ApiResponseFailSetsErrorCodeAndArabicMessage()
    {
        var response = ApiResponse<object>.Fail(ApiErrorCodes.NotFound);

        Assert.False(response.Success);
        Assert.Equal(ApiErrorCodes.NotFound, response.ErrorCode);
        Assert.Equal(ArabicErrorCatalog.GetMessage(ApiErrorCodes.NotFound), response.Message);
    }

    [Fact]
    public void AppExceptionUsesCatalogMessage()
    {
        var exception = new NotFoundException(ApiErrorCodes.AdmissionYearNotFound);

        Assert.Equal(ApiErrorCodes.AdmissionYearNotFound, exception.ErrorCode);
        Assert.Equal(ArabicErrorCatalog.GetMessage(ApiErrorCodes.AdmissionYearNotFound), exception.UserMessage);
    }
}
