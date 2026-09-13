using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tansekak.Infrastructure.Options;

namespace Tansekak.Infrastructure.Configuration;

public static class ProductionConfigurationValidator
{
    private const string DevAdminEmail = "admin@tansekak.local";
    private const string DevAdminPassword = "Admin@12345";

    public static void Validate(IConfiguration configuration, IHostEnvironment environment, ILogger logger)
    {
        if (environment.IsDevelopment())
            return;

        ValidateConnectionString(configuration);
        ValidateAdminSeed(configuration);
        WarnIfR2NotConfigured(configuration, logger);
    }

    private static void ValidateConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured in Production. " +
                "Set ConnectionStrings__DefaultConnection in MonsterASP environment variables.");
        }

        if (ContainsLocalhost(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must not point to localhost in Production. " +
                "Use the MonsterASP MSSQL connection string from the control panel.");
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
                "Set AdminSeed__Email and AdminSeed__Password in MonsterASP environment variables.");
        }

        if (string.Equals(email, DevAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "AdminSeed:Email must not use the development default in Production. " +
                "Set a unique AdminSeed__Email in MonsterASP environment variables.");
        }

        if (password == DevAdminPassword)
        {
            throw new InvalidOperationException(
                "AdminSeed:Password must not use the development default in Production. " +
                "Set a strong AdminSeed__Password in MonsterASP environment variables.");
        }
    }

    private static void WarnIfR2NotConfigured(IConfiguration configuration, ILogger logger)
    {
        var r2Options = configuration.GetSection(R2Options.SectionName).Get<R2Options>() ?? new R2Options();

        if (!r2Options.IsConfigured)
        {
            logger.LogWarning(
                "R2 storage is not configured. Excel imports over 20 MB will return HTTP 503. " +
                "Set R2__AccountId, R2__AccessKeyId, R2__SecretAccessKey, and R2__BucketName " +
                "in MonsterASP environment variables to enable large file imports.");
        }
    }

    private static bool ContainsLocalhost(string connectionString) =>
        connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
