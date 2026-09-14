using Microsoft.EntityFrameworkCore;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class DashboardService(AppDbContext db, CurrentAdmissionYearProvider yearProvider) : IDashboardService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var currentYear = await yearProvider.GetCurrentYearNumberAsync(cancellationToken);

        var governoratesCount = await db.Governorates.CountAsync(cancellationToken);
        var universitiesCount = await db.Universities.CountAsync(cancellationToken);
        var facultiesCount = await db.Faculties.CountAsync(cancellationToken);
        var universityFacultiesCount = await db.UniversityFaculties.CountAsync(cancellationToken);
        var cutoffsCount = await db.AdmissionCutoffs.CountAsync(cancellationToken);
        var studentResultsCount = await db.StudentResults.CountAsync(cancellationToken);

        return new DashboardDto(
            governoratesCount,
            universitiesCount,
            facultiesCount,
            universityFacultiesCount,
            cutoffsCount,
            studentResultsCount,
            currentYear);
    }
}
