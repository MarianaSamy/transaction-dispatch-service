using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TransactionDispatch.Application.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        Task EnqueueJobAsync(Guid jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Expose a reader so consumers (hosted services) can read enqueued job IDs.
        /// Only the background worker should read from this.
        /// </summary>
        ChannelReader<Guid> Reader { get; }
    }
}
