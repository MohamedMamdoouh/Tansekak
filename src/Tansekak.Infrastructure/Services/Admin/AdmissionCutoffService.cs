using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class AdmissionCutoffService(AppDbContext db, EntityIdAllocator idAllocator) : IAdmissionCutoffService
{
    public async Task<PagedResultDto<AdmissionCutoffDto>> GetPagedAsync(int? yearId = null, string? search = null, string? track = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);

        var query = BuildQuery(yearId, search, track);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.UniversityFaculty.University.NameAr)
            .ThenBy(x => x.UniversityFaculty.Faculty.NameAr)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdmissionCutoffDto(
                x.Id, x.AdmissionYearId, x.UniversityFacultyId,
                TrackHelper.ToDisplayName(x.Track), x.CutoffScore,
                x.UniversityFaculty.University.NameAr, x.UniversityFaculty.Faculty.NameAr))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AdmissionCutoffDto>(items, total, page, pageSize);
    }

    public async Task<AdmissionCutoffDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.AdmissionCutoffs.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AdmissionCutoffDto(
                x.Id, x.AdmissionYearId, x.UniversityFacultyId,
                TrackHelper.ToDisplayName(x.Track), x.CutoffScore,
                x.UniversityFaculty.University.NameAr, x.UniversityFaculty.Faculty.NameAr))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AdmissionCutoffDto> CreateAsync(CreateAdmissionCutoffDto dto, CancellationToken cancellationToken = default)
    {
        if (!TrackHelper.TryParse(dto.Track, out var track))
            throw new ValidationException(ApiErrorCodes.InvalidTrack);

        track = TrackHelper.Canonical(track);

        await ValidateCutoffAsync(dto.AdmissionYearId, dto.UniversityFacultyId, track, dto.CutoffScore, null, cancellationToken);

        var entity = new AdmissionCutoff
        {
            Id = await idAllocator.NextAsync(EntityIdAllocator.AdmissionCutoffs, cancellationToken),
            AdmissionYearId = dto.AdmissionYearId,
            UniversityFacultyId = dto.UniversityFacultyId,
            Track = track,
            CutoffScore = dto.CutoffScore
        };
        db.AdmissionCutoffs.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<AdmissionCutoffDto?> UpdateAsync(int id, UpdateAdmissionCutoffDto dto, CancellationToken cancellationToken = default)
    {
        if (!TrackHelper.TryParse(dto.Track, out var track))
            throw new ValidationException(ApiErrorCodes.InvalidTrack);

        track = TrackHelper.Canonical(track);

        var entity = await db.AdmissionCutoffs.FindAsync([id], cancellationToken);
        if (entity is null) return null;

        await ValidateCutoffAsync(dto.AdmissionYearId, dto.UniversityFacultyId, track, dto.CutoffScore, id, cancellationToken);

        entity.AdmissionYearId = dto.AdmissionYearId;
        entity.UniversityFacultyId = dto.UniversityFacultyId;
        entity.Track = track;
        entity.CutoffScore = dto.CutoffScore;
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.AdmissionCutoffs.FindAsync([id], cancellationToken);
        if (entity is null) return false;
        db.AdmissionCutoffs.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<AdmissionCutoff> BuildQuery(int? yearId, string? search, string? track)
    {
        var query = db.AdmissionCutoffs.AsNoTracking().AsQueryable();

        if (yearId.HasValue) query = query.Where(x => x.AdmissionYearId == yearId.Value);
        if (!string.IsNullOrWhiteSpace(track) && TrackHelper.TryParse(track, out var t))
        {
            var bucket = TrackHelper.TracksInBucket(t);
            query = query.Where(x => bucket.Contains(x.Track));
        }
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.UniversityFaculty.University.NameAr.Contains(search) || x.UniversityFaculty.Faculty.NameAr.Contains(search));

        return query;
    }

    private async Task ValidateCutoffAsync(
        int yearId,
        int universityFacultyId,
        AcademicTrack track,
        decimal cutoffScore,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        _ = await db.AdmissionYears.FindAsync([yearId], cancellationToken)
            ?? throw new NotFoundException(ApiErrorCodes.AdmissionYearNotFound);

        var universityFaculty = await db.UniversityFaculties
            .Include(x => x.Faculty)
            .FirstOrDefaultAsync(x => x.Id == universityFacultyId, cancellationToken)
            ?? throw new NotFoundException(ApiErrorCodes.UniversityFacultyNotFound);

        FacultyTrackValidator.EnsureTrackAllowed(universityFaculty.Faculty, track);

        if (cutoffScore < 0)
            throw new ValidationException(ApiErrorCodes.CutoffNegative);

        var bucket = TrackHelper.TracksInBucket(track);
        var duplicate = await db.AdmissionCutoffs.AnyAsync(x =>
            x.AdmissionYearId == yearId
            && x.UniversityFacultyId == universityFacultyId
            && bucket.Contains(x.Track)
            && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

        if (duplicate)
            throw new ValidationException(ApiErrorCodes.CutoffDuplicate);
    }
}
