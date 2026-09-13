namespace Tansekak.Infrastructure.Seeding;

public interface ISeedDataSource
{
    Task<List<T>> ReadListAsync<T>(string fileName, CancellationToken cancellationToken = default);
}
