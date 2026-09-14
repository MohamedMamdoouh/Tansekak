using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class FacultyService(AppDbContext db, EntityIdAllocator idAllocator) : IFacultyService
{
    public async Task<IReadOnlyList<FacultyDto>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var query = db.Faculties.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.NameAr.Contains(search));
        var faculties = await query.OrderBy(x => x.NameAr).ToListAsync(cancellationToken);
        return faculties.Select(ToDto).ToList();
    }

    public async Task<FacultyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Faculties.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<FacultyDto> CreateAsync(CreateFacultyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Faculty
        {
            Id = await idAllocator.NextAsync(EntityIdAllocator.Faculties, cancellationToken),
            NameAr = dto.NameAr.Trim(),
            AllowedTracks = ParseAllowedTracks(dto.AllowedTracks)
        };
        db.Faculties.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<FacultyDto?> UpdateAsync(int id, UpdateFacultyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await db.Faculties.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        entity.NameAr = dto.NameAr.Trim();
        entity.AllowedTracks = ParseAllowedTracks(dto.AllowedTracks);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static List<AcademicTrack> ParseAllowedTracks(IReadOnlyList<string> allowedTracks)
    {
        if (allowedTracks is null || allowedTracks.Count == 0)
            throw new ValidationException(ApiErrorCodes.AllowedTracksRequired);

        var tracks = new List<AcademicTrack>();
        foreach (var value in allowedTracks)
        {
            if (!TrackHelper.TryParse(value, out var track))
                throw new ValidationException(ApiErrorCodes.InvalidTrack);
            var canonical = TrackHelper.Canonical(track);
            if (!tracks.Contains(canonical))
                tracks.Add(canonical);
        }

        return tracks;
    }

    private static FacultyDto ToDto(Faculty entity) =>
        new(
            entity.Id,
            entity.NameAr,
            entity.AllowedTracks
                .Select(TrackHelper.Canonical)
                .Distinct()
                .Select(TrackHelper.ToDisplayName)
                .ToList());
}