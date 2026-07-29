using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class GovernorateService(AppDbContext db) : IGovernorateService
{
    public async Task<IReadOnlyList<GovernorateDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Governorates.AsNoTracking().OrderBy(x => x.NameAr)
            .Select(x => new GovernorateDto(x.Id, x.NameAr)).ToListAsync(cancellationToken);

    public async Task<GovernorateDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Governorates.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new GovernorateDto(x.Id, x.NameAr)).FirstOrDefaultAsync(cancellationToken);

    public async Task<GovernorateDto> CreateAsync(CreateGovernorateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Governorate { NameAr = dto.NameAr.Trim() };
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

public class UniversityService(AppDbContext db) : IUniversityService
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
        await db.Universities.AsNoTracking().Include(x => x.Governorate).Where(x => x.Id == id)
            .Select(x => new UniversityDto(x.Id, x.NameAr, x.GovernorateId, x.Type.ToString(), x.Governorate.NameAr))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UniversityDto> CreateAsync(CreateUniversityDto dto, CancellationToken cancellationToken = default)
    {
        if (!UniversityTypeHelper.TryParse(dto.Type, out var type))
            throw new ArgumentException("Invalid university type.");

        var entity = new University { NameAr = dto.NameAr.Trim(), GovernorateId = dto.GovernorateId, Type = type };
        db.Universities.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new UniversityDto(entity.Id, entity.NameAr, entity.GovernorateId, entity.Type.ToString());
    }

    public async Task<UniversityDto?> UpdateAsync(int id, UpdateUniversityDto dto, CancellationToken cancellationToken = default)
    {
        if (!UniversityTypeHelper.TryParse(dto.Type, out var type))
            throw new ArgumentException("Invalid university type.");

        var entity = await db.Universities.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        entity.NameAr = dto.NameAr.Trim();
        entity.GovernorateId = dto.GovernorateId;
        entity.Type = type;
        await db.SaveChangesAsync(cancellationToken);
        return new UniversityDto(entity.Id, entity.NameAr, entity.GovernorateId, entity.Type.ToString());
    }
}

public class FacultyService(AppDbContext db) : IFacultyService
{
    public async Task<IReadOnlyList<FacultyDto>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var query = db.Faculties.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.NameAr.Contains(search));
        return await query.OrderBy(x => x.NameAr).Select(x => new FacultyDto(x.Id, x.NameAr)).ToListAsync(cancellationToken);
    }

    public async Task<FacultyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Faculties.AsNoTracking().Where(x => x.Id == id).Select(x => new FacultyDto(x.Id, x.NameAr)).FirstOrDefaultAsync(cancellationToken);

    public async Task<FacultyDto> CreateAsync(CreateFacultyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Faculty { NameAr = dto.NameAr.Trim() };
        db.Faculties.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new FacultyDto(entity.Id, entity.NameAr);
    }

    public async Task<FacultyDto?> UpdateAsync(int id, UpdateFacultyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await db.Faculties.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        entity.NameAr = dto.NameAr.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return new FacultyDto(entity.Id, entity.NameAr);
    }
}

public class UniversityFacultyService(AppDbContext db) : IUniversityFacultyService
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
        var entity = new UniversityFaculty { UniversityId = dto.UniversityId, FacultyId = dto.FacultyId };
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

public class AdmissionYearService(AppDbContext db) : IAdmissionYearService
{
    public async Task<IReadOnlyList<AdmissionYearDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.AdmissionYears.AsNoTracking().OrderByDescending(x => x.Year)
            .Select(x => new AdmissionYearDto(x.Id, x.Year, x.MaximumScore, x.IsCurrent)).ToListAsync(cancellationToken);

    public async Task<AdmissionYearDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.AdmissionYears.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new AdmissionYearDto(x.Id, x.Year, x.MaximumScore, x.IsCurrent)).FirstOrDefaultAsync(cancellationToken);

    public async Task<AdmissionYearDto> CreateAsync(CreateAdmissionYearDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new AdmissionYear { Year = dto.Year, MaximumScore = dto.MaximumScore, IsCurrent = false };
        db.AdmissionYears.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new AdmissionYearDto(entity.Id, entity.Year, entity.MaximumScore, entity.IsCurrent);
    }

    public async Task<AdmissionYearDto?> UpdateAsync(int id, UpdateAdmissionYearDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await db.AdmissionYears.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        entity.Year = dto.Year;
        entity.MaximumScore = dto.MaximumScore;
        await db.SaveChangesAsync(cancellationToken);
        return new AdmissionYearDto(entity.Id, entity.Year, entity.MaximumScore, entity.IsCurrent);
    }

    public async Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.AdmissionYears.FindAsync([id], cancellationToken);
        if (entity is null) return false;

        var allYears = await db.AdmissionYears.ToListAsync(cancellationToken);
        foreach (var year in allYears)
            year.IsCurrent = year.Id == id;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class AdmissionCutoffService(AppDbContext db) : IAdmissionCutoffService
{
    public async Task<PagedResultDto<AdmissionCutoffDto>> GetPagedAsync(int? yearId = null, string? search = null, string? track = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

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

    public async Task<IReadOnlyList<AdmissionCutoffDto>> GetAllAsync(int? yearId = null, string? search = null, string? track = null, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(yearId, search, track);
        return await query.OrderBy(x => x.UniversityFaculty.University.NameAr)
            .Select(x => new AdmissionCutoffDto(
                x.Id, x.AdmissionYearId, x.UniversityFacultyId,
                TrackHelper.ToDisplayName(x.Track), x.CutoffScore,
                x.UniversityFaculty.University.NameAr, x.UniversityFaculty.Faculty.NameAr))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AdmissionCutoff> BuildQuery(int? yearId, string? search, string? track)
    {
        var query = db.AdmissionCutoffs.AsNoTracking()
            .Include(x => x.UniversityFaculty).ThenInclude(x => x.University)
            .Include(x => x.UniversityFaculty).ThenInclude(x => x.Faculty).AsQueryable();

        if (yearId.HasValue) query = query.Where(x => x.AdmissionYearId == yearId.Value);
        if (!string.IsNullOrWhiteSpace(track) && TrackHelper.TryParse(track, out var t))
            query = query.Where(x => x.Track == t);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.UniversityFaculty.University.NameAr.Contains(search) || x.UniversityFaculty.Faculty.NameAr.Contains(search));

        return query;
    }

    public async Task<AdmissionCutoffDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await GetAllAsync(cancellationToken: cancellationToken).ContinueWith(
            t => t.Result.FirstOrDefault(x => x.Id == id), cancellationToken);

    public async Task<AdmissionCutoffDto> CreateAsync(CreateAdmissionCutoffDto dto, CancellationToken cancellationToken = default)
    {
        if (!TrackHelper.TryParse(dto.Track, out var track))
            throw new ArgumentException("Invalid track.");

        await ValidateCutoffAsync(dto.AdmissionYearId, dto.UniversityFacultyId, track, dto.CutoffScore, null, cancellationToken);

        var entity = new AdmissionCutoff
        {
            AdmissionYearId = dto.AdmissionYearId,
            UniversityFacultyId = dto.UniversityFacultyId,
            Track = track,
            CutoffScore = dto.CutoffScore
        };
        db.AdmissionCutoffs.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetAllAsync(dto.AdmissionYearId, cancellationToken: cancellationToken))
            .First(x => x.Id == entity.Id);
    }

    public async Task<AdmissionCutoffDto?> UpdateAsync(int id, UpdateAdmissionCutoffDto dto, CancellationToken cancellationToken = default)
    {
        if (!TrackHelper.TryParse(dto.Track, out var track))
            throw new ArgumentException("Invalid track.");

        var entity = await db.AdmissionCutoffs.FindAsync([id], cancellationToken);
        if (entity is null) return null;

        await ValidateCutoffAsync(dto.AdmissionYearId, dto.UniversityFacultyId, track, dto.CutoffScore, id, cancellationToken);

        entity.AdmissionYearId = dto.AdmissionYearId;
        entity.UniversityFacultyId = dto.UniversityFacultyId;
        entity.Track = track;
        entity.CutoffScore = dto.CutoffScore;
        await db.SaveChangesAsync(cancellationToken);
        return (await GetAllAsync(dto.AdmissionYearId, cancellationToken: cancellationToken)).FirstOrDefault(x => x.Id == id);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.AdmissionCutoffs.FindAsync([id], cancellationToken);
        if (entity is null) return false;
        db.AdmissionCutoffs.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateCutoffAsync(int yearId, int ufId, AcademicTrack track, decimal score, int? excludeId, CancellationToken ct)
    {
        var year = await db.AdmissionYears.FindAsync([yearId], ct) ?? throw new ArgumentException("Admission year not found.");
        if (score <= 0 || score > year.MaximumScore)
            throw new ArgumentException($"Score must be between 0 and {year.MaximumScore}.");

        var faculty = await db.UniversityFaculties.AsNoTracking()
            .Include(x => x.Faculty)
            .Where(x => x.Id == ufId)
            .Select(x => x.Faculty)
            .FirstOrDefaultAsync(ct)
            ?? throw new ArgumentException("University faculty not found.");

        FacultyTrackValidator.EnsureTrackAllowed(faculty, track);

        var exists = await db.AdmissionCutoffs.AnyAsync(
            x => x.AdmissionYearId == yearId && x.UniversityFacultyId == ufId && x.Track == track && x.Id != excludeId, ct);
        if (exists) throw new ArgumentException("Duplicate cutoff record.");
    }
}
