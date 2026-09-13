using Microsoft.EntityFrameworkCore;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class AdmissionYearServiceTests
{
    [Fact]
    public async Task PublishAsync_sets_only_one_current_year()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            db.AdmissionYears.AddRange(
                new AdmissionYear { Id = 1, Year = 2025, MaximumScore = 320, IsCurrent = true },
                new AdmissionYear { Id = 2, Year = 2026, MaximumScore = 320, IsCurrent = false });
            await db.SaveChangesAsync();

            var service = new AdmissionYearService(db, new EntityIdAllocator(db));
            var published = await service.PublishAsync(2);
            Assert.True(published);

            var currentYears = await db.AdmissionYears.Where(x => x.IsCurrent).ToListAsync();
            Assert.Single(currentYears);
            Assert.Equal(2, currentYears[0].Id);
        }
    }
}
