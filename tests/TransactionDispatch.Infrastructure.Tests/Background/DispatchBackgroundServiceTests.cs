using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Infrastructure.Background;
using Xunit;

namespace TransactionDispatch.Infrastructure.Tests.Background
{
    public class DispatchBackgroundServiceTests
    {
        private static (BackgroundTaskQueue queue, Mock<IServiceScopeFactory> scopeFactory, Mock<IJobProcessor> jobProcessor) CreateEnvironment()
        {
            var queue = new BackgroundTaskQueue();
            var jobProcessor = new Mock<IJobProcessor>();

            // Create a scope whose ServiceProvider returns the mocked jobProcessor
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(sp => sp.GetService(typeof(IJobProcessor)))
                .Returns(jobProcessor.Object);

            // Some DI helpers call GetRequiredService<T>() extension that uses GetService under the hood in tests,
            // but we will setup ServiceProvider.GetRequiredService via IServiceProvider as used below.
            var scope = new Mock<IServiceScope>();
            scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

            return (queue, scopeFactory, jobProcessor);
        }

        [Fact]
        public async Task ExecuteAsync_ProcessesEnqueuedJob()
        {
            // Arrange
            var (queue, scopeFactory, jobProcessor) = CreateEnvironment();
            var logger = new Mock<ILogger<DispatchBackgroundService>>().Object;

            var tcs = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
            jobProcessor
                .Setup(p => p.ProcessJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns<Guid, CancellationToken>((id, ct) =>
                {
                    tcs.TrySetResult(id);
                    return Task.CompletedTask;
                });

            var service = new DispatchBackgroundService(queue, scopeFactory.Object, logger);

            var jobId = Guid.NewGuid();
            await queue.EnqueueJobAsync(jobId);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Act - start the background service (this will call ExecuteAsync internally)
            var start = service.StartAsync(cts.Token);

            // Wait until ProcessJobAsync was invoked (or timeout)
            var processedId = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5), cts.Token));
            Assert.True(tcs.Task.IsCompleted, "Timed out waiting for job to be processed.");

            // Stop the service
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);

            // Assert
            jobProcessor.Verify(p => p.ProcessJobAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
