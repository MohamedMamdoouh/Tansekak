using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Common;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Seeding;

public sealed class CutoffBootstrapService(
    AppDbContext db,
    IImportService importService,
    CurrentAdmissionYearProvider yearProvider,
    ILogger<CutoffBootstrapService> logger)
{
    public async Task<int> BootstrapMissingTracksAsync(CancellationToken cancellationToken = default)
    {
        if (!await db.AdmissionYears.AnyAsync(cancellationToken))
        {
            logger.LogDebug("Skipping cutoff bootstrap because no admission years exist yet.");
            return 0;
        }

        AdmissionYear currentYear;
        try
        {
            currentYear = await yearProvider.GetCurrentAsync(cancellationToken);
        }
        catch (ServiceUnavailableException)
        {
            logger.LogWarning("Skipping cutoff bootstrap because no current admission year is configured.");
            return 0;
        }

        var cutoffsDirectory = SeedDirectoryLocator.TryResolveCutoffsDirectory();
        if (cutoffsDirectory is null)
        {
            logger.LogWarning("Cutoff bootstrap skipped because seed cutoff files were not found on disk.");
            return 0;
        }

        var importedTracks = 0;

        foreach (var trackName in TrackHelper.AllTracks)
        {
            if (!TrackHelper.TryParse(trackName, out var track))
                continue;

            var existingCount = await db.AdmissionCutoffs.CountAsync(
                cutoff => cutoff.AdmissionYearId == currentYear.Id && cutoff.Track == track,
                cancellationToken);

            if (existingCount > 0)
                continue;

            var seedFilePath = CutoffSeedFileResolver.Resolve(cutoffsDirectory, track, currentYear.Year);
            if (seedFilePath is null)
            {
                logger.LogWarning(
                    "No seed cutoff file found for track {Track} and year {Year}.",
                    trackName,
                    currentYear.Year);
                continue;
            }

            await using var stream = File.OpenRead(seedFilePath);
            var result = await importService.ImportAsync(
                currentYear.Id,
                trackName,
                stream,
                Path.GetFileName(seedFilePath),
                cancellationToken);

            if (!result.Success)
            {
                var details = result.Errors is { Count: > 0 } errors
                    ? string.Join("; ", errors.Select(error => $"{error.RowNumber}:{error.ErrorCode}"))
                    : result.Message;
                logger.LogError(
                    "Cutoff bootstrap import failed for track {Track}: {Details}",
                    trackName,
                    details);
                continue;
            }

            importedTracks++;
            logger.LogInformation(
                "Bootstrapped {Count} cutoffs for track {Track} from {FileName}.",
                result.ImportedCount,
                trackName,
                Path.GetFileName(seedFilePath));
        }

        return importedTracks;
    }
}
