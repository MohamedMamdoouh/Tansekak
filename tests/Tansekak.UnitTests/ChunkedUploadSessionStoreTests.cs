using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Tansekak.Infrastructure.Services;

namespace Tansekak.UnitTests;

public class ChunkedUploadSessionStoreTests
{
    [Fact]
    public void CreateSession_RejectsNonXlsxExtension()
    {
        var store = CreateStore();

        var ex = Assert.Throws<ArgumentException>(() =>
            store.CreateSession(1, "results.csv", 1024, 1));

        Assert.Contains(".xlsx", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryCompleteSession_FailsWhenChunksMissing()
    {
        var store = CreateStore();
        var session = store.CreateSession(1, "results.xlsx", 10, 2);

        var (saved, saveError) = await store.TrySaveChunkAsync(
            session.UploadId, 0, ToStream([1, 2, 3, 4, 5]), CancellationToken.None);
        Assert.True(saved);
        Assert.Null(saveError);

        var (job, completeError) = await store.TryCompleteSessionAsync(
            session.UploadId, CancellationToken.None);

        Assert.Null(job);
        Assert.NotNull(completeError);
        Assert.Contains("Missing chunks", completeError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryCompleteSession_MergesChunksInOrder()
    {
        var store = CreateStore();
        var chunk0 = new byte[] { 1, 2, 3 };
        var chunk1 = new byte[] { 4, 5 };
        var session = store.CreateSession(1, "results.xlsx", chunk0.Length + chunk1.Length, 2);

        Assert.True((await store.TrySaveChunkAsync(session.UploadId, 0, ToStream(chunk0), CancellationToken.None)).Success);
        Assert.True((await store.TrySaveChunkAsync(session.UploadId, 1, ToStream(chunk1), CancellationToken.None)).Success);

        var (job, completeError) = await store.TryCompleteSessionAsync(
            session.UploadId, CancellationToken.None);

        Assert.Null(completeError);
        Assert.NotNull(job);
        Assert.Equal("processing", job.Status);

        var assembledPath = Path.Combine(Path.GetTempPath(), $"tansekak-import-{session.UploadId:N}.xlsx");
        try
        {
            Assert.True(File.Exists(assembledPath));
            Assert.Equal([1, 2, 3, 4, 5], File.ReadAllBytes(assembledPath));
        }
        finally
        {
            if (File.Exists(assembledPath))
                File.Delete(assembledPath);
        }
    }

    private static ChunkedUploadSessionStore CreateStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var jobQueue = new StudentResultImportJobQueue(scopeFactory, NullLogger<StudentResultImportJobQueue>.Instance);
        return new ChunkedUploadSessionStore(jobQueue, NullLogger<ChunkedUploadSessionStore>.Instance);
    }

    private static MemoryStream ToStream(byte[] bytes) => new(bytes);
}
