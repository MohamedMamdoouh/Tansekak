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
    ImportJobCancellationRegistry cancellationRegistry,
    ILogger<ImportJobService> logger) : IImportJobService
{
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

    public async Task<ImportJobDto?> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await db.ImportJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null)
            return null;

        if (job.Status is ImportJobStatus.Cancelled
            or ImportJobStatus.Completed
            or ImportJobStatus.Failed)
        {
            return ToDto(job);
        }

        // Signal any in-flight worker first so claim races still observe cancel.
        cancellationRegistry.RequestCancel(jobId);

        var cancelledMessage = ArabicErrorCatalog.GetMessage(ApiErrorCodes.ImportJobCancelled);
        var completedAt = DateTime.UtcNow;
        var cancelled = await db.ImportJobs
            .Where(x => x.Id == jobId && x.Status == ImportJobStatus.Queued)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.Status, ImportJobStatus.Cancelled)
                .SetProperty(j => j.Message, cancelledMessage)
                .SetProperty(j => j.CompletedAtUtc, completedAt),
                cancellationToken);

        if (cancelled > 0)
        {
            cancellationRegistry.TakePendingCancel(jobId);
            await TryDeleteObjectAsync(job.ObjectKey, jobId, cancellationToken);
            return await GetAsync(jobId, cancellationToken);
        }

        // Already Running (or just claimed): cooperative cancel via registry.
        // ProcessJobAsync will persist Cancelled when it observes the signal.
        return await GetAsync(jobId, cancellationToken);
    }

    public async Task PrepareQueueAsync(CancellationToken cancellationToken = default)
    {
        // PrepareQueueAsync runs only at process startup. Any Status=Running row is
        // orphaned from a prior crash/deploy — there is no live worker still holding it.
        // Filtering by age left recently-interrupted imports stuck Running forever
        // (timeout is never re-checked while the process stays up).
        var interruptedRunning = await db.ImportJobs
            .Where(x => x.Status == ImportJobStatus.Running)
            .Select(x => new { x.Id, x.AdmissionYearId, x.CreatedAtUtc, x.ObjectKey })
            .ToListAsync(cancellationToken);

        if (interruptedRunning.Count > 0)
        {
            var requeueIds = new List<Guid>();
            var superseded = new List<(Guid Id, string? ObjectKey)>();

            foreach (var interrupted in interruptedRunning)
            {
                // A newer job for the same year means the admin already moved on
                // (retry after crash). Re-running the old R2 object would full-replace
                // student results and silently overwrite the newer import.
                var hasNewerJob = await db.ImportJobs.AnyAsync(
                    x => x.AdmissionYearId == interrupted.AdmissionYearId
                        && x.Id != interrupted.Id
                        && x.CreatedAtUtc > interrupted.CreatedAtUtc,
                    cancellationToken);

                if (hasNewerJob)
                    superseded.Add((interrupted.Id, interrupted.ObjectKey));
                else
                    requeueIds.Add(interrupted.Id);
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
                    "Marked {Count} superseded interrupted import jobs as failed to protect newer imports.",
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
                    "Requeued {Count} import jobs left Running after process restart.",
                    requeueIds.Count);
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

        using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var registration = cancellationRegistry.RegisterRunning(jobId, jobCts);

        // AsNoTracking: StudentResultImportService.ImportAsync calls ChangeTracker.Clear()
        // after each batch on this same scoped DbContext, which would detach a tracked job
        // and make a later SaveChangesAsync silently skip the Completed/Failed write —
        // leaving the job stuck in Running while the SPA poll loop waits forever.
        var job = await db.ImportJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null)
            return;

        var objectKey = job.ObjectKey;
        var userCancelled = false;
        try
        {
            if (cancellationRegistry.TakePendingCancel(jobId) || jobCts.IsCancellationRequested)
            {
                await MarkCancelledAsync(jobId, cancellationToken);
                userCancelled = true;
                return;
            }

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

            await using var stream = await r2Storage.OpenReadAsync(objectKey, jobCts.Token);
            var fileName = Path.GetFileName(objectKey);
            var result = await importService.ImportAsync(job.AdmissionYearId, stream, fileName, jobCts.Token);

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
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            userCancelled = true;
            await MarkCancelledAsync(jobId, CancellationToken.None);
            logger.LogInformation("Import job {JobId} cancelled by admin.", jobId);
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
            // Always delete the staged object once the job reaches a terminal outcome.
            // User-cancel and normal completion/failure all land here after status is set.
            if (userCancelled
                || !cancellationToken.IsCancellationRequested)
            {
                await TryDeleteObjectAsync(objectKey, jobId, CancellationToken.None);
            }
        }
    }

    private async Task MarkCancelledAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var cancelledMessage = ArabicErrorCatalog.GetMessage(ApiErrorCodes.ImportJobCancelled);
        var completedAt = DateTime.UtcNow;
        await db.ImportJobs
            .Where(x => x.Id == jobId
                && (x.Status == ImportJobStatus.Queued || x.Status == ImportJobStatus.Running))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.Status, ImportJobStatus.Cancelled)
                .SetProperty(j => j.Message, cancelledMessage)
                .SetProperty(j => j.CompletedAtUtc, completedAt),
                cancellationToken);
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
