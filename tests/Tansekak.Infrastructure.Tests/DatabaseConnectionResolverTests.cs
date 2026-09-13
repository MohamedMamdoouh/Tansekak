using Microsoft.Extensions.Configuration;
using Npgsql;
using Tansekak.Infrastructure.Configuration;

namespace Tansekak.Infrastructure.Tests;

public class DatabaseConnectionResolverTests
{
    [Fact]
    public void Normalize_converts_neon_uri_to_npgsql_keywords()
    {
        var normalized = DatabaseConnectionResolver.Normalize(
            "postgresql://app_user:p%40ss@ep-example-pooler.c-2.us-east-2.aws.neon.tech/neondb?sslmode=require");

        var builder = new NpgsqlConnectionStringBuilder(normalized);

        Assert.Equal("ep-example-pooler.c-2.us-east-2.aws.neon.tech", builder.Host);
        Assert.Equal("neondb", builder.Database);
        Assert.Equal("app_user", builder.Username);
        Assert.Equal("p@ss", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Normalize_leaves_keyword_connection_string_unchanged()
    {
        const string keywordForm =
            "Host=localhost;Port=5432;Database=Tansekak;Username=postgres;Password=postgres";

        Assert.Equal(keywordForm, DatabaseConnectionResolver.Normalize(keywordForm));
    }

    [Fact]
    public void Resolve_converts_uri_in_default_connection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "postgres://app_user:secret@db.example.com/appdb"
            })
            .Build();

        var resolved = DatabaseConnectionResolver.Resolve(config);
        var builder = new NpgsqlConnectionStringBuilder(resolved);

        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal("appdb", builder.Database);
        Assert.Equal("app_user", builder.Username);
        Assert.Equal("secret", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Resolve_falls_back_to_database_url()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgresql://app_user:secret@db.example.com/appdb"
            })
            .Build();

        var resolved = DatabaseConnectionResolver.Resolve(config);
        var builder = new NpgsqlConnectionStringBuilder(resolved);

        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal("appdb", builder.Database);
    }
}
