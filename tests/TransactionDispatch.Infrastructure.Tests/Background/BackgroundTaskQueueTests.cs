using System;
using System.Threading;
using System.Threading.Tasks;
using TransactionDispatch.Infrastructure.Background;
using Xunit;

namespace TransactionDispatch.Infrastructure.Tests.Background
{
    public class BackgroundTaskQueueTests
    {
        [Fact]
        public async Task EnqueueJobAsync_ValidGuid_ReaderReceivesIt()
        {
            // Arrange
            var queue = new BackgroundTaskQueue();
            var id = Guid.NewGuid();

            // Act
            await queue.EnqueueJobAsync(id);

            // Read with timeout to avoid hanging test if something's wrong
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var read = await queue.Reader.ReadAsync(cts.Token).AsTask();

            // Assert
            Assert.Equal(id, read);
        }

        [Fact]
        public async Task EnqueueJobAsync_EmptyGuid_ThrowsArgumentException()
        {
            var queue = new BackgroundTaskQueue();
            await Assert.ThrowsAsync<ArgumentException>(() => queue.EnqueueJobAsync(Guid.Empty));
        }

        [Fact]
        public async Task EnqueueJobAsync_Multiple_Items_AreReadInOrder()
        {
            // Arrange
            var queue = new BackgroundTaskQueue();
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var c = Guid.NewGuid();

            // Act
            await queue.EnqueueJobAsync(a);
            await queue.EnqueueJobAsync(b);
            await queue.EnqueueJobAsync(c);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var gotA = await queue.Reader.ReadAsync(cts.Token).AsTask();
            var gotB = await queue.Reader.ReadAsync(cts.Token).AsTask();
            var gotC = await queue.Reader.ReadAsync(cts.Token).AsTask();

            // Assert
            Assert.Equal(a, gotA);
            Assert.Equal(b, gotB);
            Assert.Equal(c, gotC);
        }
    }
}
