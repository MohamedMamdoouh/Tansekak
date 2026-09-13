using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Tansekak.Infrastructure.Persistence;

public sealed class EntityIdAllocator(AppDbContext db)
{
    public const string AdmissionYears = "AdmissionYears";
    public const string AdmissionCutoffs = "AdmissionCutoffs";
    public const string Faculties = "Faculties";
    public const string Governorates = "Governorates";
    public const string StudentResults = "StudentResults";
    public const string Universities = "Universities";
    public const string UniversityFaculties = "UniversityFaculties";

    private static readonly HashSet<string> KnownSequences =
    [
        AdmissionYears,
        AdmissionCutoffs,
        Faculties,
        Governorates,
        StudentResults,
        Universities,
        UniversityFaculties
    ];

    public async Task<int> NextAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        var (startId, _) = await AllocateRangeAsync(sequenceName, 1, cancellationToken);
        return startId;
    }

    public async Task<(int startId, int count)> AllocateRangeAsync(
        string sequenceName,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (!KnownSequences.Contains(sequenceName))
            throw new ArgumentException($"Unknown sequence name '{sequenceName}'.", nameof(sequenceName));

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var ownsTransaction = db.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
            transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await EnsureSequenceRowAsync(sequenceName, cancellationToken);

            var updated = await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE EntityIdSequences
                 SET NextId = NextId + {count}
                 WHERE Name = {sequenceName}
                 """,
                cancellationToken);

            if (updated != 1)
                throw new InvalidOperationException($"Failed to allocate ids for sequence '{sequenceName}'.");

            var nextId = await db.EntityIdSequences
                .AsNoTracking()
                .Where(x => x.Name == sequenceName)
                .Select(x => x.NextId)
                .FirstAsync(cancellationToken);

            if (ownsTransaction && transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return (nextId - count, count);
        }
        catch
        {
            if (ownsTransaction && transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (ownsTransaction)
                transaction?.Dispose();
        }
    }

    private async Task EnsureSequenceRowAsync(string sequenceName, CancellationToken cancellationToken)
    {
        var exists = await db.EntityIdSequences.AnyAsync(x => x.Name == sequenceName, cancellationToken);
        if (exists)
            return;

        var sql =
            "IF NOT EXISTS (SELECT 1 FROM EntityIdSequences WHERE Name = {0}) " +
            $"BEGIN INSERT INTO EntityIdSequences (Name, NextId) SELECT {{0}}, ISNULL(MAX(Id), 0) + 1 FROM [{sequenceName}] END";

        await db.Database.ExecuteSqlRawAsync(sql, [sequenceName], cancellationToken);
    }
}
