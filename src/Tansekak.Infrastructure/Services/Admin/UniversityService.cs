using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class UniversityService(AppDbContext db, EntityIdAllocator idAllocator) : IUniversityService
{
    public async Task<IReadOnlyList<UniversityDto>> GetAllAsync(string? search = null, int? governorateId = null, string? type = null, CancellationToken cancellationToken = default)
    {
        var query = db.Universities.AsNoTracking().Include(x => x.Governorate).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.NameAr.Contains(search));
        if (governorateId.HasValue)
            query = query.Where(x => x.GovernorateId == governorateId.Value);
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<UniversityType>(type, true, out var ut))
            query = query.Where(x => x.Type == ut);

        return await query.OrderBy(x => x.NameAr)
            .Select(x => new UniversityDto(x.Id, x.NameAr, x.GovernorateId, x.Type.ToString(), x.Governorate.NameAr))
            .ToListAsync(cancellationToken);
    }

    public async Task<UniversityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ServiceGuards.NotFoundIfNull(
            await db.Universities.AsNoTracking().Include(x => x.Governorate).Where(x => x.Id == id)
                .Select(x => new UniversityDto(x.Id, x.NameAr, x.GovernorateId, x.Type.ToString(), x.Governorate.NameAr))
                .FirstOrDefaultAsync(cancellationToken));

    public async Task<UniversityDto> CreateAsync(CreateUniversityDto dto, CancellationToken cancellationToken = default)
    {
        if (!UniversityTypeHelper.TryParse(dto.Type, out var type))
            throw new ValidationException(ApiErrorCodes.InvalidUniversityType);

        await CatalogReferenceValidator.EnsureGovernorateExistsAsync(db, dto.GovernorateId, cancellationToken);

        var entity = new University
        {
            Id = await idAllocator.NextAsync(EntityIdAllocator.Universities, cancellationToken),
            NameAr = dto.NameAr.Trim(),
            GovernorateId = dto.GovernorateId,
            Type = type
        };
        db.Universities.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new UniversityDto(entity.Id, entity.NameAr, entity.GovernorateId, entity.Type.ToString());
    }

    public async Task<UniversityDto?> UpdateAsync(int id, UpdateUniversityDto dto, CancellationToken cancellationToken = default)
    {
        if (!UniversityTypeHelper.TryParse(dto.Type, out var type))
            throw new ValidationException(ApiErrorCodes.InvalidUniversityType);

        var entity = await db.Universities.FindAsync([id], cancellationToken);
        ServiceGuards.NotFoundIfNull(entity);

        await CatalogReferenceValidator.EnsureGovernorateExistsAsync(db, dto.GovernorateId, cancellationToken);

        entity!.NameAr = dto.NameAr.Trim();
        entity.GovernorateId = dto.GovernorateId;
        entity.Type = type;
        await db.SaveChangesAsync(cancellationToken);
        return new UniversityDto(entity.Id, entity.NameAr, entity.GovernorateId, entity.Type.ToString());
    }
}