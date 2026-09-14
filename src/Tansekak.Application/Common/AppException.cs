using System.Net;

namespace Tansekak.Application.Common;

public abstract class AppException : Exception
{
    protected AppException(string errorCode, HttpStatusCode statusCode, string? userMessage = null)
        : base(userMessage ?? ArabicErrorCatalog.GetMessage(errorCode))
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        UserMessage = userMessage ?? ArabicErrorCatalog.GetMessage(errorCode);
    }

    protected AppException(string errorCode, HttpStatusCode statusCode, string? userMessage, Exception innerException)
        : base(userMessage ?? ArabicErrorCatalog.GetMessage(errorCode), innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        UserMessage = userMessage ?? ArabicErrorCatalog.GetMessage(errorCode);
    }

    public string ErrorCode { get; }

    public HttpStatusCode StatusCode { get; }

    public string UserMessage { get; }
}
