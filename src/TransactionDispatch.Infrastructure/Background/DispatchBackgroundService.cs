using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TransactionDispatch.Application.Interfaces;

namespace TransactionDispatch.Infrastructure.Background
{
    public class DispatchBackgroundService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DispatchBackgroundService> _logger;

        public DispatchBackgroundService(
            IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory scopeFactory,
            ILogger<DispatchBackgroundService> logger)
        {
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DispatchBackgroundService started.");
            var reader = _taskQueue.Reader;

            await foreach (var jobId in reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                // create a scope per job to resolve scoped dependencies
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var jobProcessor = scope.ServiceProvider.GetRequiredService<IJobProcessor>();

                    // run job processor (await so we process sequentially). If you want parallel job processing,
                    // you can Task.Run here and let it run without awaiting, but watch resource limits.
                    await jobProcessor.ProcessJobAsync(jobId, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancellation requested while handling job {JobId}", jobId);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error while processing job {JobId}", jobId);
                }
            }

            _logger.LogInformation("DispatchBackgroundService stopped.");
        }
    }
}
