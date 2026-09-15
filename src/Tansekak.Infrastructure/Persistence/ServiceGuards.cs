using Tansekak.Application.Common;

namespace Tansekak.Infrastructure.Persistence;

internal static class ServiceGuards
{
    public static T NotFoundIfNull<T>(T? value, string errorCode = ApiErrorCodes.NotFound)
        where T : class
    {
        if (value is null)
            throw new NotFoundException(errorCode);

        return value;
    }
}
