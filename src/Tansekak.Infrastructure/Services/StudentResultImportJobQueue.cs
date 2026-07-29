using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;

namespace Tansekak.Infrastructure.Services;

public sealed class ImportJobState
{
    public required Guid JobId { get; init; }
    public required int YearId { get; init; }
    public string Status { get; set; } = "processing";
    public ImportResultDto? Result { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public class StudentResultImportJobQueue(
    IServiceScopeFactory scopeFactory,
    ILogger<StudentResultImportJobQueue> logger)
{
    private static readonly TimeSpan JobRetention = TimeSpan.FromHours(6);
    private readonly ConcurrentDictionary<Guid, ImportJobState> _jobs = new();

    public ImportJobState Enqueue(int yearId, string tempFilePath, string fileName)
    {
        CleanupExpiredJobs();

        var jobId = Guid.NewGuid();
        var state = new ImportJobState
        {
            JobId = jobId,
            YearId = yearId,
            Status = "processing",
            Message = "Import queued."
        };
        _jobs[jobId] = state;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var importService = scope.ServiceProvider.GetRequiredService<IStudentResultImportService>();

                await using var stream = File.OpenRead(tempFilePath);
                var result = await importService.ImportAsync(yearId, stream, fileName, CancellationToken.None);

                state.Status = result.Success ? "completed" : "failed";
                state.Result = result;
                state.Message = result.Message;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Student result import job {JobId} failed.", jobId);
                state.Status = "failed";
                state.Message = "An unexpected error occurred during import.";
                state.Result = new ImportResultDto(false, state.Message);
            }
            finally
            {
                TryDeleteFile(tempFilePath, logger);
            }
        });

        return state;
    }

    public ImportJobState? GetJob(Guid jobId)
    {
        _jobs.TryGetValue(jobId, out var state);
        return state;
    }

    private void CleanupExpiredJobs()
    {
        var cutoff = DateTimeOffset.UtcNow - JobRetention;
        foreach (var (jobId, state) in _jobs)
        {
            if (state.CreatedAt < cutoff)
                _jobs.TryRemove(jobId, out _);
        }
    }

    private static void TryDeleteFile(string path, ILogger logger)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete import temp file {TempFilePath}.", path);
        }
    }
}
