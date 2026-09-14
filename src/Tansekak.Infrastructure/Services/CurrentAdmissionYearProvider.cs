using Microsoft.EntityFrameworkCore;
using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class CurrentAdmissionYearProvider(AppDbContext db)
{
    public async Task<AdmissionYear> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        await db.AdmissionYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsCurrent, cancellationToken)
            ?? throw new ServiceUnavailableException(ApiErrorCodes.NoCurrentYear);

    public async Task<int?> GetCurrentYearNumberAsync(CancellationToken cancellationToken = default) =>
        await db.AdmissionYears.AsNoTracking()
            .Where(x => x.IsCurrent).Select(x => (int?)x.Year).FirstOrDefaultAsync(cancellationToken);
}
