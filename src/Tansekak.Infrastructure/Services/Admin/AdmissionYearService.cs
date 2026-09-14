using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class AdmissionYearService(AppDbContext db, EntityIdAllocator idAllocator) : IAdmissionYearService
{
    public async Task<IReadOnlyList<AdmissionYearDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.AdmissionYears.AsNoTracking().OrderByDescending(x => x.Year)
            .Select(x => new AdmissionYearDto(x.Id, x.Year, x.MaximumScore, x.IsCurrent)).ToListAsync(cancellationToken);

    public async Task<AdmissionYearDto?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        await db.AdmissionYears.AsNoTracking()
            .Where(x => x.IsCurrent)
            .Select(x => new AdmissionYearDto(x.Id, x.Year, x.MaximumScore, x.IsCurrent))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AdmissionYearDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.AdmissionYears.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new AdmissionYearDto(x.Id, x.Year, x.MaximumScore, x.IsCurrent)).FirstOrDefaultAsync(cancellationToken);

    public async Task<AdmissionYearDto> CreateAsync(CreateAdmissionYearDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureYearUniqueAsync(dto.Year, null, cancellationToken);

        var entity = new AdmissionYear
        {
            Id = await idAllocator.NextAsync(EntityIdAllocator.AdmissionYears, cancellationToken),
            Year = dto.Year,
            MaximumScore = dto.MaximumScore,
            IsCurrent = false
        };
        db.AdmissionYears.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new AdmissionYearDto(entity.Id, entity.Year, entity.MaximumScore, entity.IsCurrent);
    }

    public async Task<AdmissionYearDto?> UpdateAsync(int id, UpdateAdmissionYearDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await db.AdmissionYears.FindAsync([id], cancellationToken);
        if (entity is null) return null;

        await EnsureYearUniqueAsync(dto.Year, id, cancellationToken);

        entity.Year = dto.Year;
        entity.MaximumScore = dto.MaximumScore;
        await db.SaveChangesAsync(cancellationToken);
        return new AdmissionYearDto(entity.Id, entity.Year, entity.MaximumScore, entity.IsCurrent);
    }

    public async Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var entity = await db.AdmissionYears.FindAsync([id], cancellationToken);
        if (entity is null)
            return false;

        // Exclude the target row: ExecuteUpdate bypasses the change tracker, so clearing
        // IsCurrent on the tracked entity leaves SaveChanges with nothing to write back
        // when re-publishing an already-current year (resulting in zero current years).
        await db.AdmissionYears
            .Where(x => x.IsCurrent && x.Id != id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsCurrent, false), cancellationToken);

        entity.IsCurrent = true;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task EnsureYearUniqueAsync(int year, int? excludeId, CancellationToken cancellationToken)
    {
        var duplicate = await db.AdmissionYears.AnyAsync(
            x => x.Year == year && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

        if (duplicate)
            throw new ValidationException(ApiErrorCodes.AdmissionYearDuplicate);
    }
}
