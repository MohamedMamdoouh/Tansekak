using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Common;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Seeding;

/// <summary>
/// Restores Mathematics student tracks that were collapsed to Science by
/// <c>CollapseMathematicsIntoScience</c>, using CaseDesc / seating inference
/// (same rules as import) while leaving true Science rows alone.
/// </summary>
public sealed class StudentMathematicsTrackRepairService(
    AppDbContext db,
    ILogger<StudentMathematicsTrackRepairService> logger)
{
    public async Task<int> RepairAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await db.StudentResults
            .Where(result => result.Track == AcademicTrack.Science)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return 0;

        var repaired = 0;
        foreach (var result in candidates)
        {
            var inferred =
                StudentTrackInferrer.TryInferFromCaseDesc(result.StudentCaseDesc)
                ?? SeatingNumberTrackInferrer.TryInferFromSeatingNo(result.SeatingNo);

            if (inferred != AcademicTrack.Mathematics)
                continue;

            result.Track = AcademicTrack.Mathematics;
            repaired++;
        }

        if (repaired > 0)
            await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Student Mathematics track repair completed. Updated {Count} results.",
            repaired);
        return repaired;
    }
}
