using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Tansekak.Infrastructure.Configuration;

public static class DatabaseConnectionResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var databaseUrl = configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(databaseUrl))
            return string.Empty;

        return ParseDatabaseUrl(databaseUrl);
    }

    internal static string ParseDatabaseUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
