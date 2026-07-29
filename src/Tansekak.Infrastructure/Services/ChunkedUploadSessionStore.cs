using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Tansekak.Application.DTOs;

namespace Tansekak.Infrastructure.Services;

public sealed class ChunkedUploadSession
{
    public required Guid UploadId { get; init; }
    public required int YearId { get; init; }
    public required string FileName { get; init; }
    public required long TotalSize { get; init; }
    public required int TotalChunks { get; init; }
    public required string TempDirectory { get; init; }
    public HashSet<int> ReceivedChunks { get; } = [];
    public object SyncRoot { get; } = new();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public class ChunkedUploadSessionStore(
    StudentResultImportJobQueue jobQueue,
    ILogger<ChunkedUploadSessionStore> logger)
{
    public const int ChunkSizeBytes = 5 * 1024 * 1024;
    public const long MaxFileSizeBytes = 104_857_600;
    private static readonly TimeSpan SessionRetention = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<Guid, ChunkedUploadSession> _sessions = new();

    public ImportUploadSessionDto CreateSession(int yearId, string fileName, long totalSize, int totalChunks)
    {
        CleanupExpiredSessions();

        if (totalSize <= 0 || totalSize > MaxFileSizeBytes)
            throw new ArgumentException($"File size must be between 1 and {MaxFileSizeBytes} bytes.");

        if (totalChunks <= 0)
            throw new ArgumentException("Total chunks must be greater than zero.");

        var ext = Path.GetExtension(fileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .xlsx files are supported.");

        var uploadId = Guid.NewGuid();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tansekak-upload-{uploadId:N}");
        Directory.CreateDirectory(tempDirectory);

        var session = new ChunkedUploadSession
        {
            UploadId = uploadId,
            YearId = yearId,
            FileName = fileName,
            TotalSize = totalSize,
            TotalChunks = totalChunks,
            TempDirectory = tempDirectory
        };
        _sessions[uploadId] = session;

        logger.LogInformation(
            "Created chunked upload session {UploadId} for year {YearId}, {TotalChunks} chunks, {TotalSize} bytes.",
            uploadId,
            yearId,
            totalChunks,
            totalSize);

        return new ImportUploadSessionDto(uploadId, ChunkSizeBytes);
    }

    public async Task<(bool Success, string? Error)> TrySaveChunkAsync(
        Guid uploadId,
        int chunkIndex,
        Stream chunkStream,
        CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(uploadId, out var session))
            return (false, "Upload session not found.");

        if (chunkIndex < 0 || chunkIndex >= session.TotalChunks)
            return (false, "Chunk index is out of range.");

        var chunkPath = GetChunkPath(session, chunkIndex);
        await using (var output = File.Create(chunkPath))
        {
            await chunkStream.CopyToAsync(output, cancellationToken);
        }

        lock (session.SyncRoot)
        {
            session.ReceivedChunks.Add(chunkIndex);
        }

        return (true, null);
    }

    public async Task<(ImportJobState? Job, string? Error)> TryCompleteSessionAsync(
        Guid uploadId,
        CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(uploadId, out var session))
            return (null, "Upload session not found.");

        lock (session.SyncRoot)
        {
            if (session.ReceivedChunks.Count != session.TotalChunks)
                return (null, $"Missing chunks: received {session.ReceivedChunks.Count} of {session.TotalChunks}.");

            for (var i = 0; i < session.TotalChunks; i++)
            {
                if (!session.ReceivedChunks.Contains(i))
                    return (null, $"Missing chunk {i}.");
            }
        }

        var assembledPath = Path.Combine(Path.GetTempPath(), $"tansekak-import-{uploadId:N}.xlsx");
        try
        {
            await MergeChunksAsync(session, assembledPath, cancellationToken);
            var job = jobQueue.Enqueue(session.YearId, assembledPath, session.FileName);
            logger.LogInformation(
                "Completed chunked upload {UploadId} and queued import job {JobId}.",
                uploadId,
                job.JobId);
            return (job, null);
        }
        finally
        {
            RemoveSession(uploadId);
        }
    }

    public bool TryAbortSession(Guid uploadId) => RemoveSession(uploadId);

    private bool RemoveSession(Guid uploadId)
    {
        if (!_sessions.TryRemove(uploadId, out var session))
            return false;

        TryDeleteDirectory(session.TempDirectory);
        return true;
    }

    private static async Task MergeChunksAsync(
        ChunkedUploadSession session,
        string outputPath,
        CancellationToken cancellationToken)
    {
        await using var output = File.Create(outputPath);
        for (var i = 0; i < session.TotalChunks; i++)
        {
            var chunkPath = GetChunkPath(session, i);
            await using var input = File.OpenRead(chunkPath);
            await input.CopyToAsync(output, cancellationToken);
        }
    }

    private static string GetChunkPath(ChunkedUploadSession session, int chunkIndex) =>
        Path.Combine(session.TempDirectory, $"chunk-{chunkIndex:D6}.part");

    private void CleanupExpiredSessions()
    {
        var cutoff = DateTimeOffset.UtcNow - SessionRetention;
        foreach (var (uploadId, session) in _sessions)
        {
            if (session.CreatedAt >= cutoff) continue;
            if (_sessions.TryRemove(uploadId, out var expired))
                TryDeleteDirectory(expired.TempDirectory);
        }
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete chunked upload directory {UploadDirectory}.", path);
        }
    }
}
