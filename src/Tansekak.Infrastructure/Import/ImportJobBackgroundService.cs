using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tansekak.Application.Interfaces;

namespace Tansekak.Infrastructure.Import;

public class ImportJobBackgroundService(
    ImportJobQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportJobBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var jobService = scope.ServiceProvider.GetRequiredService<IImportJobService>();
            await jobService.PrepareQueueAsync(stoppingToken);
        }

        await foreach (var jobId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var jobService = scope.ServiceProvider.GetRequiredService<IImportJobService>();
                await jobService.ProcessJobAsync(jobId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process import job {JobId}.", jobId);
            }
        }
    }
}
