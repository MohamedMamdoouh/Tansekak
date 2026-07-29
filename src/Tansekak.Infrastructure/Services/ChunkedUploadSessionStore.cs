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

    public bool TrySaveChunk(Guid uploadId, int chunkIndex, Stream chunkStream, out string? error)
    {
        error = null;
        if (!_sessions.TryGetValue(uploadId, out var session))
        {
            error = "Upload session not found.";
            return false;
        }

        if (chunkIndex < 0 || chunkIndex >= session.TotalChunks)
        {
            error = "Chunk index is out of range.";
            return false;
        }

        var chunkPath = GetChunkPath(session, chunkIndex);
        lock (session.SyncRoot)
        {
            using var output = File.Create(chunkPath);
            chunkStream.CopyTo(output);
            session.ReceivedChunks.Add(chunkIndex);
        }

        return true;
    }

    public ImportJobState? TryCompleteSession(Guid uploadId, out string? error)
    {
        error = null;
        if (!_sessions.TryGetValue(uploadId, out var session))
        {
            error = "Upload session not found.";
            return null;
        }

        lock (session.SyncRoot)
        {
            if (session.ReceivedChunks.Count != session.TotalChunks)
            {
                error = $"Missing chunks: received {session.ReceivedChunks.Count} of {session.TotalChunks}.";
                return null;
            }

            for (var i = 0; i < session.TotalChunks; i++)
            {
                if (!session.ReceivedChunks.Contains(i))
                {
                    error = $"Missing chunk {i}.";
                    return null;
                }
            }
        }

        var assembledPath = Path.Combine(Path.GetTempPath(), $"tansekak-import-{uploadId:N}.xlsx");
        try
        {
            MergeChunks(session, assembledPath);
            var job = jobQueue.Enqueue(session.YearId, assembledPath, session.FileName);
            logger.LogInformation(
                "Completed chunked upload {UploadId} and queued import job {JobId}.",
                uploadId,
                job.JobId);
            return job;
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

    private static void MergeChunks(ChunkedUploadSession session, string outputPath)
    {
        using var output = File.Create(outputPath);
        for (var i = 0; i < session.TotalChunks; i++)
        {
            var chunkPath = GetChunkPath(session, i);
            using var input = File.OpenRead(chunkPath);
            input.CopyTo(output);
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
