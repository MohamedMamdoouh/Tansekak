using System.Net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tansekak.Application.Common;

namespace Tansekak.Infrastructure.Persistence;

public static class DbUpdateExceptionMapper
{
    public static (HttpStatusCode StatusCode, string ErrorCode) Map(DbUpdateException exception)
    {
        var sqlState = FindPostgresSqlState(exception);
        return sqlState switch
        {
            PostgresErrorCodes.UniqueViolation => (HttpStatusCode.BadRequest, ApiErrorCodes.Duplicate),
            PostgresErrorCodes.ForeignKeyViolation => (HttpStatusCode.BadRequest, ApiErrorCodes.ValidationFailed),
            _ => (HttpStatusCode.InternalServerError, ApiErrorCodes.InternalError)
        };
    }

    private static string? FindPostgresSqlState(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
                return postgres.SqlState;
        }

        return null;
    }
}
