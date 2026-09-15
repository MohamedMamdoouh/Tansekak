using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
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
    /// <summary>
    /// SQL-translatable prefilter for Science rows that may have been collapsed
    /// Mathematics. Over-fetches a little (e.g. Science CaseDesc + Math seating);
    /// <see cref="RepairAsync"/> still confirms with the authoritative inferrers.
    /// </summary>
    private static readonly Expression<Func<StudentResult, bool>> LooksLikeCollapsedMathematics =
        result =>
            // CaseDesc Math markers (raw + ى variants; C# inferrer also normalizes alef).
            result.StudentCaseDesc.Contains("علمي رياضة")
            || result.StudentCaseDesc.Contains("علمي رياضه")
            || result.StudentCaseDesc.Contains("علمى رياضة")
            || result.StudentCaseDesc.Contains("علمى رياضه")
            // 2026 seating: 2nd digit 4-6 ⇒ Mathematics (CaseDesc still wins in-process).
            || (result.SeatingNo.Length == 7
                && (result.SeatingNo.StartsWith("24")
                    || result.SeatingNo.StartsWith("25")
                    || result.SeatingNo.StartsWith("26")));

    public async Task<int> RepairAsync(CancellationToken cancellationToken = default)
    {
        // Never materialize the full Science cohort. Large national imports can be
        // hundreds of thousands of rows; loading them on every startup OOMs the
        // process (small Render instances especially) and crash-loops before the API binds.
        // Pre-filter to rows that *might* need repair, then confirm with the same
        // CaseDesc-first / seating inferrers used at import time.
        var candidates = await db.StudentResults
            .Where(result => result.Track == AcademicTrack.Science)
            .Where(LooksLikeCollapsedMathematics)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            logger.LogInformation(
                "Student Mathematics track repair completed. Updated {Count} results.",
                0);
            return 0;
        }

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
