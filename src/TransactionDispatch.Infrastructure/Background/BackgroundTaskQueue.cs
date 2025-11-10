using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TransactionDispatch.Application.Interfaces;

namespace TransactionDispatch.Infrastructure.Background
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Guid> _queue;

        public BackgroundTaskQueue(int capacity = 1000)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };

            _queue = Channel.CreateBounded<Guid>(options);
        }

        public ChannelReader<Guid> Reader => _queue.Reader;

        public async Task EnqueueJobAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            if (jobId == Guid.Empty)
                throw new ArgumentException("Invalid job ID.", nameof(jobId));

            await _queue.Writer.WriteAsync(jobId, cancellationToken).ConfigureAwait(false);
        }
    }
}
