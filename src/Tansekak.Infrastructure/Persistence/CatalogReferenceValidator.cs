using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;

namespace Tansekak.Infrastructure.Persistence;

internal static class CatalogReferenceValidator
{
    public static async Task EnsureGovernorateExistsAsync(
        AppDbContext db,
        int governorateId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Governorates.AsNoTracking().AnyAsync(x => x.Id == governorateId, cancellationToken))
            throw new NotFoundException(ApiErrorCodes.NotFound);
    }

    public static async Task EnsureUniversityExistsAsync(
        AppDbContext db,
        int universityId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Universities.AsNoTracking().AnyAsync(x => x.Id == universityId, cancellationToken))
            throw new NotFoundException(ApiErrorCodes.NotFound);
    }

    public static async Task EnsureFacultyExistsAsync(
        AppDbContext db,
        int facultyId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Faculties.AsNoTracking().AnyAsync(x => x.Id == facultyId, cancellationToken))
            throw new NotFoundException(ApiErrorCodes.NotFound);
    }

    public static async Task EnsureUniversityFacultyPairUniqueAsync(
        AppDbContext db,
        int universityId,
        int facultyId,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.UniversityFaculties.AsNoTracking()
            .AnyAsync(
                x => x.UniversityId == universityId
                    && x.FacultyId == facultyId
                    && (!excludeId.HasValue || x.Id != excludeId.Value),
                cancellationToken);

        if (exists)
            throw new ValidationException(ApiErrorCodes.DuplicateRow);
    }
}
