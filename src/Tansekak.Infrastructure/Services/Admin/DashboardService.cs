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

        var governoratesTask = db.Governorates.CountAsync(cancellationToken);
        var universitiesTask = db.Universities.CountAsync(cancellationToken);
        var facultiesTask = db.Faculties.CountAsync(cancellationToken);
        var universityFacultiesTask = db.UniversityFaculties.CountAsync(cancellationToken);
        var cutoffsTask = db.AdmissionCutoffs.CountAsync(cancellationToken);
        var studentResultsTask = db.StudentResults.CountAsync(cancellationToken);

        await Task.WhenAll(
            governoratesTask,
            universitiesTask,
            facultiesTask,
            universityFacultiesTask,
            cutoffsTask,
            studentResultsTask);

        return new DashboardDto(
            await governoratesTask,
            await universitiesTask,
            await facultiesTask,
            await universityFacultiesTask,
            await cutoffsTask,
            await studentResultsTask,
            currentYear);
    }
}
