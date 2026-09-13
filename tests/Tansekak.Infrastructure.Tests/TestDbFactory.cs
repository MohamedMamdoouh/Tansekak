using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Tests;

internal static class TestDbFactory
{
    public static (AppDbContext Db, SqliteConnection Connection) Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return (db, connection);
    }
}
