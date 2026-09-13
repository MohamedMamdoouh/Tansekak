using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class UniversityFacultyService(AppDbContext db, EntityIdAllocator idAllocator) : IUniversityFacultyService
{
    public async Task<IReadOnlyList<UniversityFacultyDto>> GetAllAsync(string? search = null, int? universityId = null, int? facultyId = null, CancellationToken cancellationToken = default)
    {
        var query = db.UniversityFaculties.AsNoTracking()
            .Include(x => x.University).Include(x => x.Faculty).AsQueryable();
        if (universityId.HasValue) query = query.Where(x => x.UniversityId == universityId.Value);
        if (facultyId.HasValue) query = query.Where(x => x.FacultyId == facultyId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.University.NameAr.Contains(search) || x.Faculty.NameAr.Contains(search));

        return await query.OrderBy(x => x.University.NameAr).ThenBy(x => x.Faculty.NameAr)
            .Select(x => new UniversityFacultyDto(x.Id, x.UniversityId, x.FacultyId, x.University.NameAr, x.Faculty.NameAr))
            .ToListAsync(cancellationToken);
    }

    public async Task<UniversityFacultyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.UniversityFaculties.AsNoTracking().Include(x => x.University).Include(x => x.Faculty)
            .Where(x => x.Id == id)
            .Select(x => new UniversityFacultyDto(x.Id, x.UniversityId, x.FacultyId, x.University.NameAr, x.Faculty.NameAr))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UniversityFacultyDto> CreateAsync(CreateUniversityFacultyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new UniversityFaculty
        {
            Id = await idAllocator.NextAsync(EntityIdAllocator.UniversityFaculties, cancellationToken),
            UniversityId = dto.UniversityId,
            FacultyId = dto.FacultyId
        };
        db.UniversityFaculties.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<UniversityFacultyDto?> UpdateAsync(int id, UpdateUniversityFacultyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await db.UniversityFaculties.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        entity.UniversityId = dto.UniversityId;
        entity.FacultyId = dto.FacultyId;
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }
}