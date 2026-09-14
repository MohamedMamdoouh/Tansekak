using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Persistence;

namespace Tansekak.Infrastructure.Services;

public class ImportJobService(
    AppDbContext db,
    IR2Storage r2Storage,
    IStudentResultImportService importService,
    ImportJobQueue queue,
    ILogger<ImportJobService> logger) : IImportJobService
{
    public static readonly TimeSpan StaleRunningJobTimeout = TimeSpan.FromHours(2);

    public async Task<ImportJobDto> CreateQueuedJobAsync(
        int yearId,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        _ = await db.AdmissionYears.FindAsync([yearId], cancellationToken)
            ?? throw new NotFoundException(ApiErrorCodes.AdmissionYearNotFound);

        if (string.IsNullOrWhiteSpace(objectKey)
            || !objectKey.StartsWith($"imports/{yearId}/", StringComparison.Ordinal))
        {
            throw new ValidationException(ApiErrorCodes.InvalidObjectKey);
        }

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            AdmissionYearId = yearId,
            Status = ImportJobStatus.Queued,
            ObjectKey = objectKey,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ImportJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        await queue.EnqueueAsync(job.Id, cancellationToken);
        return ToDto(job);
    }

    public async Task<ImportJobDto?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await db.ImportJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        return job is null ? null : ToDto(job);
    }

    public async Task PrepareQueueAsync(CancellationToken cancellationToken = default)
    {
        var staleCutoff = DateTime.UtcNow.Subtract(StaleRunningJobTimeout);

        var staleRunning = await db.ImportJobs
            .Where(x => x.Status == ImportJobStatus.Running
                && x.StartedAtUtc != null
                && x.StartedAtUtc < staleCutoff)
            .Select(x => new { x.Id, x.AdmissionYearId, x.CreatedAtUtc, x.ObjectKey })
            .ToListAsync(cancellationToken);

        if (staleRunning.Count > 0)
        {
            var requeueIds = new List<Guid>();
            var superseded = new List<(Guid Id, string? ObjectKey)>();

            foreach (var stale in staleRunning)
            {
                // A newer job for the same year means the admin already moved on
                // (retry after crash). Re-running the old R2 object would full-replace
                // student results and silently overwrite the newer import.
                var hasNewerJob = await db.ImportJobs.AnyAsync(
                    x => x.AdmissionYearId == stale.AdmissionYearId
                        && x.Id != stale.Id
                        && x.CreatedAtUtc > stale.CreatedAtUtc,
                    cancellationToken);

                if (hasNewerJob)
                    superseded.Add((stale.Id, stale.ObjectKey));
                else
                    requeueIds.Add(stale.Id);
            }

            if (superseded.Count > 0)
            {
                var supersededIds = superseded.Select(x => x.Id).ToList();
                var failedMessage = ArabicErrorCatalog.GetMessage(ApiErrorCodes.ImportJobSuperseded);
                var completedAt = DateTime.UtcNow;

                await db.ImportJobs
                    .Where(x => supersededIds.Contains(x.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(j => j.Status, ImportJobStatus.Failed)
                        .SetProperty(j => j.Message, failedMessage)
                        .SetProperty(j => j.CompletedAtUtc, completedAt),
                        cancellationToken);

                foreach (var (id, objectKey) in superseded)
                    await TryDeleteObjectAsync(objectKey, id, cancellationToken);

                logger.LogWarning(
                    "Marked {Count} superseded stale import jobs as failed to protect newer imports.",
                    superseded.Count);
            }

            if (requeueIds.Count > 0)
            {
                await db.ImportJobs
                    .Where(x => requeueIds.Contains(x.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(j => j.Status, ImportJobStatus.Queued)
                        .SetProperty(j => j.StartedAtUtc, (DateTime?)null),
                        cancellationToken);

                logger.LogWarning(
                    "Requeued {Count} stale import jobs that were running longer than {Timeout}.",
                    requeueIds.Count,
                    StaleRunningJobTimeout);
            }
        }

        var pendingJobIds = await db.ImportJobs
            .Where(x => x.Status == ImportJobStatus.Queued)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var jobId in pendingJobIds)
            await queue.EnqueueAsync(jobId, cancellationToken);

        if (pendingJobIds.Count > 0)
        {
            logger.LogInformation("Rehydrated {Count} queued import jobs.", pendingJobIds.Count);
        }
    }

    public async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var claimed = await db.ImportJobs
            .Where(x => x.Id == jobId && x.Status == ImportJobStatus.Queued)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.Status, ImportJobStatus.Running)
                .SetProperty(j => j.StartedAtUtc, startedAt),
                cancellationToken);

        if (claimed == 0)
            return;

        // AsNoTracking: StudentResultImportService.ImportAsync calls ChangeTracker.Clear()
        // after each batch on this same scoped DbContext, which would detach a tracked job
        // and make a later SaveChangesAsync silently skip the Completed/Failed write —
        // leaving the job stuck in Running while the SPA poll loop waits forever.
        var job = await db.ImportJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null)
            return;

        var objectKey = job.ObjectKey;
        try
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ValidationException(ApiErrorCodes.ImportJobMissingKey);

            // Defense in depth: even if a stale job was requeued, never full-replace
            // results when a newer job for the same year already exists.
            var hasNewerJob = await db.ImportJobs.AsNoTracking().AnyAsync(
                x => x.AdmissionYearId == job.AdmissionYearId
                    && x.Id != jobId
                    && x.CreatedAtUtc > job.CreatedAtUtc,
                cancellationToken);
            if (hasNewerJob)
            {
                var supersededMessage = ArabicErrorCatalog.GetMessage(ApiErrorCodes.ImportJobSuperseded);
                var supersededAt = DateTime.UtcNow;
                await db.ImportJobs
                    .Where(x => x.Id == jobId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(j => j.Status, ImportJobStatus.Failed)
                        .SetProperty(j => j.Message, supersededMessage)
                        .SetProperty(j => j.CompletedAtUtc, supersededAt),
                        cancellationToken);
                logger.LogWarning(
                    "Skipped import job {JobId}; a newer job exists for year {YearId}.",
                    jobId,
                    job.AdmissionYearId);
                return;
            }

            await using var stream = await r2Storage.OpenReadAsync(objectKey, cancellationToken);
            var fileName = Path.GetFileName(objectKey);
            var result = await importService.ImportAsync(job.AdmissionYearId, stream, fileName, cancellationToken);

            var completedAt = DateTime.UtcNow;
            var status = result.Success ? ImportJobStatus.Completed : ImportJobStatus.Failed;
            await db.ImportJobs
                .Where(x => x.Id == jobId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.ImportedCount, result.ImportedCount)
                    .SetProperty(j => j.Message, result.Message)
                    .SetProperty(j => j.Status, status)
                    .SetProperty(j => j.CompletedAtUtc, completedAt),
                    cancellationToken);

            if (!result.Success)
                logger.LogWarning("Import job {JobId} failed validation.", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import job {JobId} failed.", jobId);
            var failedMessage = ArabicErrorCatalog.GetMessage(ApiErrorCodes.InternalError);
            var completedAt = DateTime.UtcNow;
            await db.ImportJobs
                .Where(x => x.Id == jobId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.Status, ImportJobStatus.Failed)
                    .SetProperty(j => j.Message, failedMessage)
                    .SetProperty(j => j.CompletedAtUtc, completedAt),
                    cancellationToken);
        }
        finally
        {
            await TryDeleteObjectAsync(objectKey, jobId, cancellationToken);
        }
    }

    private async Task TryDeleteObjectAsync(
        string? objectKey,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return;

        try
        {
            await r2Storage.DeleteAsync(objectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete R2 object {ObjectKey} for job {JobId}.", objectKey, jobId);
        }
    }

    private static ImportJobDto ToDto(ImportJob job) =>
        new(job.Id, job.Status, job.ImportedCount, job.Message, job.CreatedAtUtc, job.CompletedAtUtc);
}
