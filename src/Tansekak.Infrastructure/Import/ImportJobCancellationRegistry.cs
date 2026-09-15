using System.Collections.Concurrent;

namespace Tansekak.Infrastructure.Import;

/// <summary>
/// Cross-scope cancel coordination for import jobs. <see cref="ImportJobService"/> is scoped
/// (HTTP cancel vs background worker), so the registry must be a singleton.
/// </summary>
public sealed class ImportJobCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, byte> _pending = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();

    public void RequestCancel(Guid jobId)
    {
        _pending[jobId] = 0;
        if (_running.TryGetValue(jobId, out var cts))
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Worker already finished disposing the CTS.
            }
        }
    }

    public bool TakePendingCancel(Guid jobId) => _pending.TryRemove(jobId, out _);

    public IDisposable RegisterRunning(Guid jobId, CancellationTokenSource cts)
    {
        _running[jobId] = cts;
        if (_pending.ContainsKey(jobId))
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        return new Registration(this, jobId);
    }

    private sealed class Registration(ImportJobCancellationRegistry registry, Guid jobId) : IDisposable
    {
        public void Dispose() => registry._running.TryRemove(jobId, out _);
    }
}
