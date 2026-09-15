using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class ImportJobServiceTests
{
    [Fact]
    public async Task PrepareQueueAsync_rehydrates_queued_jobs()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedAdmissionYear(db);
            var queue = new ImportJobQueue();
            var jobId = Guid.NewGuid();
            db.ImportJobs.Add(new ImportJob
            {
                Id = jobId,
                AdmissionYearId = 1,
                Status = ImportJobStatus.Queued,
                ObjectKey = "imports/1/test.xlsx",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService(db, queue);
            await service.PrepareQueueAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(jobId, enumerator.Current);
            await enumerator.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessJobAsync_persists_completed_status_when_import_clears_change_tracker()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedAdmissionYear(db);
            var queue = new ImportJobQueue();
            var jobId = Guid.NewGuid();
            db.ImportJobs.Add(new ImportJob
            {
                Id = jobId,
                AdmissionYearId = 1,
                Status = ImportJobStatus.Queued,
                ObjectKey = "imports/1/test.xlsx",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            // Mirrors StudentResultImportService batching: Clear() detaches tracked entities
            // on the shared scoped DbContext after the job row was loaded.
            var importService = new ClearingImportService(db);
            var service = CreateService(db, queue, importService);

            await service.ProcessJobAsync(jobId);

            var job = await db.ImportJobs.AsNoTracking().SingleAsync(x => x.Id == jobId);
            Assert.Equal(ImportJobStatus.Completed, job.Status);
            Assert.Equal(3, job.ImportedCount);
            Assert.Equal("cleared-ok", job.Message);
            Assert.NotNull(job.CompletedAtUtc);
        }
    }

    [Fact]
    public async Task ProcessJobAsync_claims_only_one_worker_for_same_job()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedAdmissionYear(db);
            var queue = new ImportJobQueue();
            var jobId = Guid.NewGuid();
            db.ImportJobs.Add(new ImportJob
            {
                Id = jobId,
                AdmissionYearId = 1,
                Status = ImportJobStatus.Queued,
                ObjectKey = "imports/1/test.xlsx",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var importService = new FakeImportService();
            var service = CreateService(db, queue, importService);

            await Task.WhenAll(
                service.ProcessJobAsync(jobId),
                service.ProcessJobAsync(jobId));

            Assert.Equal(1, importService.CallCount);
            // ExecuteUpdate does not refresh tracked entities; read from the database.
            var job = await db.ImportJobs.AsNoTracking().SingleAsync(x => x.Id == jobId);
            Assert.Equal(ImportJobStatus.Completed, job.Status);
        }
    }

    [Fact]
    public async Task PrepareQueueAsync_fails_interrupted_running_job_when_newer_job_exists()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedAdmissionYear(db);
            var queue = new ImportJobQueue();
            var interruptedJobId = Guid.NewGuid();
            var newerJobId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            db.ImportJobs.AddRange(
                new ImportJob
                {
                    Id = interruptedJobId,
                    AdmissionYearId = 1,
                    Status = ImportJobStatus.Running,
                    ObjectKey = "imports/1/old.xlsx",
                    CreatedAtUtc = now.AddHours(-5),
                    StartedAtUtc = now.AddHours(-3)
                },
                new ImportJob
                {
                    Id = newerJobId,
                    AdmissionYearId = 1,
                    Status = ImportJobStatus.Completed,
                    ObjectKey = "imports/1/new.xlsx",
                    CreatedAtUtc = now.AddHours(-1),
                    CompletedAtUtc = now.AddMinutes(-50),
                    ImportedCount = 10
                });
            await db.SaveChangesAsync();

            var r2 = new FakeR2Storage();
            var service = CreateService(db, queue, r2: r2);
            await service.PrepareQueueAsync();

            var interrupted = await db.ImportJobs.AsNoTracking().SingleAsync(x => x.Id == interruptedJobId);
            Assert.Equal(ImportJobStatus.Failed, interrupted.Status);
            Assert.Contains("استُبدلت", interrupted.Message);
            Assert.Contains("imports/1/old.xlsx", r2.DeletedKeys);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in queue.ReadAllAsync(cts.Token))
                {
                }
            });
        }
    }

    [Fact]
    public async Task PrepareQueueAsync_requeues_interrupted_running_job_when_no_newer_job()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedAdmissionYear(db);
            var queue = new ImportJobQueue();
            var interruptedJobId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            db.ImportJobs.Add(new ImportJob
            {
                Id = interruptedJobId,
                AdmissionYearId = 1,
                Status = ImportJobStatus.Running,
                ObjectKey = "imports/1/old.xlsx",
                CreatedAtUtc = now.AddHours(-5),
                StartedAtUtc = now.AddHours(-3)
            });
            await db.SaveChangesAsync();

            var service = CreateService(db, queue);
            await service.PrepareQueueAsync();

            var interrupted = await db.ImportJobs.AsNoTracking().SingleAsync(x => x.Id == interruptedJobId);
            Assert.Equal(ImportJobStatus.Queued, interrupted.Status);
            Assert.Null(interrupted.StartedAtUtc);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(interruptedJobId, enumerator.Current);
            await enumerator.DisposeAsync();
        }
    }

    [Fact]
    public async Task PrepareQueueAsync_requeues_recently_started_running_job_on_startup()
    {
        // Crash/redeploy mid-import leaves Status=Running with a fresh StartedAtUtc.
        // PrepareQueueAsync only runs at startup, so age filters incorrectly left these
        // jobs stuck Running forever (SPA poll never completes).
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedAdmissionYear(db);
            var queue = new ImportJobQueue();
            var jobId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            db.ImportJobs.Add(new ImportJob
            {
                Id = jobId,
                AdmissionYearId = 1,
                Status = ImportJobStatus.Running,
                ObjectKey = "imports/1/recent.xlsx",
                CreatedAtUtc = now.AddMinutes(-15),
                StartedAtUtc = now.AddMinutes(-10)
            });
            await db.SaveChangesAsync();

            var service = CreateService(db, queue);
            await service.PrepareQueueAsync();

            var job = await db.ImportJobs.AsNoTracking().SingleAsync(x => x.Id == jobId);
            Assert.Equal(ImportJobStatus.Queued, job.Status);
            Assert.Null(job.StartedAtUtc);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(jobId, enumerator.Current);
            await enumerator.DisposeAsync();
        }
    }

    [Fact]
    public async Task PrepareQueueAsync_fails_recent_running_job_when_newer_completed_exists()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedAdmissionYear(db);
            var queue = new ImportJobQueue();
            var interruptedJobId = Guid.NewGuid();
            var newerJobId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            db.ImportJobs.AddRange(
                new ImportJob
                {
                    Id = interruptedJobId,
                    AdmissionYearId = 1,
                    Status = ImportJobStatus.Running,
                    ObjectKey = "imports/1/old.xlsx",
                    CreatedAtUtc = now.AddMinutes(-40),
                    StartedAtUtc = now.AddMinutes(-30)
                },
                new ImportJob
                {
                    Id = newerJobId,
                    AdmissionYearId = 1,
                    Status = ImportJobStatus.Completed,
                    ObjectKey = "imports/1/new.xlsx",
                    CreatedAtUtc = now.AddMinutes(-10),
                    CompletedAtUtc = now.AddMinutes(-5),
                    ImportedCount = 10
                });
            await db.SaveChangesAsync();

            var r2 = new FakeR2Storage();
            var service = CreateService(db, queue, r2: r2);
            await service.PrepareQueueAsync();

            var interrupted = await db.ImportJobs.AsNoTracking().SingleAsync(x => x.Id == interruptedJobId);
            Assert.Equal(ImportJobStatus.Failed, interrupted.Status);
            Assert.Contains("استُبدلت", interrupted.Message);
            Assert.Contains("imports/1/old.xlsx", r2.DeletedKeys);
        }
    }

    [Fact]
    public async Task ProcessJobAsync_skips_import_when_newer_job_exists()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            SeedAdmissionYear(db);
            var queue = new ImportJobQueue();
            var oldJobId = Guid.NewGuid();
            var newerJobId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            db.ImportJobs.AddRange(
                new ImportJob
                {
                    Id = oldJobId,
                    AdmissionYearId = 1,
                    Status = ImportJobStatus.Queued,
                    ObjectKey = "imports/1/old.xlsx",
                    CreatedAtUtc = now.AddHours(-2)
                },
                new ImportJob
                {
                    Id = newerJobId,
                    AdmissionYearId = 1,
                    Status = ImportJobStatus.Completed,
                    ObjectKey = "imports/1/new.xlsx",
                    CreatedAtUtc = now.AddMinutes(-10),
                    CompletedAtUtc = now.AddMinutes(-5),
                    ImportedCount = 42
                });
            await db.SaveChangesAsync();

            var importService = new FakeImportService();
            var r2 = new FakeR2Storage();
            var service = CreateService(db, queue, importService, r2);

            await service.ProcessJobAsync(oldJobId);

            Assert.Equal(0, importService.CallCount);
            var oldJob = await db.ImportJobs.AsNoTracking().SingleAsync(x => x.Id == oldJobId);
            Assert.Equal(ImportJobStatus.Failed, oldJob.Status);
            Assert.Contains("استُبدلت", oldJob.Message);
            Assert.Contains("imports/1/old.xlsx", r2.DeletedKeys);
        }
    }

    private static ImportJobService CreateService(
        AppDbContext db,
        ImportJobQueue queue,
        IStudentResultImportService? importService = null,
        FakeR2Storage? r2 = null)
    {
        SeedAdmissionYear(db);

        return new ImportJobService(
            db,
            r2 ?? new FakeR2Storage(),
            importService ?? new FakeImportService(),
            queue,
            NullLogger<ImportJobService>.Instance);
    }

    private static void SeedAdmissionYear(AppDbContext db)
    {
        if (!db.AdmissionYears.Any())
        {
            db.AdmissionYears.Add(new AdmissionYear
            {
                Id = 1,
                Year = 2026,
                MaximumScore = 320,
                IsCurrent = true
            });
            db.SaveChanges();
        }
    }

    private sealed class ClearingImportService(AppDbContext db) : IStudentResultImportService
    {
        public Task<ImportResultDto> ImportAsync(
            int yearId,
            Stream fileStream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            db.ChangeTracker.Clear();
            return Task.FromResult(new ImportResultDto(true, "cleared-ok", 3));
        }
    }

    private sealed class FakeImportService : IStudentResultImportService
    {
        public int CallCount { get; private set; }

        public Task<ImportResultDto> ImportAsync(
            int yearId,
            Stream fileStream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ImportResultDto(true, "ok", 1));
        }
    }

    private sealed class FakeR2Storage : IR2Storage
    {
        public bool IsConfigured => true;
        public List<string> DeletedKeys { get; } = [];

        public Task<(string UploadUrl, string ObjectKey)> CreatePresignedUploadAsync(
            int yearId,
            string fileName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(("https://example.com", "imports/1/test.xlsx"));

        public Task UploadAsync(
            string objectKey,
            Stream stream,
            string contentType,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
            return Task.FromResult<Stream>(stream);
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            DeletedKeys.Add(objectKey);
            return Task.CompletedTask;
        }
    }
}
