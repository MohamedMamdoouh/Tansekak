using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
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

    [Fact]
    public async Task CreateAsync_rejects_duplicate_year()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            db.AdmissionYears.Add(new AdmissionYear { Id = 1, Year = 2026, MaximumScore = 320, IsCurrent = true });
            await db.SaveChangesAsync();

            var service = new AdmissionYearService(db, new EntityIdAllocator(db));
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => service.CreateAsync(new CreateAdmissionYearDto(2026, 320)));

            Assert.Equal(ApiErrorCodes.AdmissionYearDuplicate, ex.ErrorCode);
        }
    }

    [Fact]
    public async Task UpdateAsync_rejects_duplicate_year()
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
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => service.UpdateAsync(2, new UpdateAdmissionYearDto(2025, 320)));

            Assert.Equal(ApiErrorCodes.AdmissionYearDuplicate, ex.ErrorCode);
        }
    }
}
