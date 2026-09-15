using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tansekak.Infrastructure.Configuration;

public static class ProductionConfigurationValidator
{
    private const string DevAdminEmail = "admin@tansekak.local";
    private const string DevAdminPassword = "Admin@12345";

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return;

        ValidateConnectionString(configuration);
        ValidateAdminSeed(configuration);
    }

    private static void ValidateConnectionString(IConfiguration configuration)
    {
        var connectionString = DatabaseConnectionResolver.Resolve(configuration);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured in Production. " +
                "Set ConnectionStrings__DefaultConnection or DATABASE_URL in Render environment variables.");
        }

        if (ContainsLocalhost(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must not point to localhost in Production. " +
                "Use the Neon Postgres connection string from the Neon dashboard.");
        }
    }

    private static void ValidateAdminSeed(IConfiguration configuration)
    {
        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "AdminSeed:Email and AdminSeed:Password must be configured in Production. " +
                "Set AdminSeed__Email and AdminSeed__Password in Render environment variables.");
        }

        if (string.Equals(email, DevAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "AdminSeed:Email must not use the development default in Production. " +
                "Set a unique AdminSeed__Email in Render environment variables.");
        }

        if (password == DevAdminPassword)
        {
            throw new InvalidOperationException(
                "AdminSeed:Password must not use the development default in Production. " +
                "Set a strong AdminSeed__Password in Render environment variables.");
        }
    }

    private static bool ContainsLocalhost(string connectionString) =>
        connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
