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

    private static ImportJobService CreateService(
        AppDbContext db,
        ImportJobQueue queue,
        IStudentResultImportService? importService = null)
    {
        SeedAdmissionYear(db);

        return new ImportJobService(
            db,
            new FakeR2Storage(),
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

        public Task<(string UploadUrl, string ObjectKey)> CreatePresignedUploadAsync(
            int yearId,
            string fileName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(("https://example.com", "imports/1/test.xlsx"));

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
            return Task.FromResult<Stream>(stream);
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
