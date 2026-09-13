using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class GovernorateService(AppDbContext db, EntityIdAllocator idAllocator) : IGovernorateService
{
    public async Task<IReadOnlyList<GovernorateDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Governorates.AsNoTracking().OrderBy(x => x.NameAr)
            .Select(x => new GovernorateDto(x.Id, x.NameAr)).ToListAsync(cancellationToken);

    public async Task<GovernorateDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Governorates.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new GovernorateDto(x.Id, x.NameAr)).FirstOrDefaultAsync(cancellationToken);

    public async Task<GovernorateDto> CreateAsync(CreateGovernorateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Governorate
        {
            Id = await idAllocator.NextAsync(EntityIdAllocator.Governorates, cancellationToken),
            NameAr = dto.NameAr.Trim()
        };
        db.Governorates.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new GovernorateDto(entity.Id, entity.NameAr);
    }

    public async Task<GovernorateDto?> UpdateAsync(int id, UpdateGovernorateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await db.Governorates.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        entity.NameAr = dto.NameAr.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return new GovernorateDto(entity.Id, entity.NameAr);
    }
}