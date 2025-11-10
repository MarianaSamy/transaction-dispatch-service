using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionDispatch.Application.Interfaces
{
    /// <summary>
    /// Simple abstraction to produce a file payload to Kafka.
    /// Implementations should be resilient and honor cancellation tokens.
    /// </summary>
    public interface IKafkaProducer
    {
        /// <summary>
        /// Produce the provided stream payload to Kafka. The implementation may read the stream fully.
        /// </summary>
        /// <param name="payloadStream">Stream positioned at start.</param>
        /// <param name="key">Message key (e.g., filename)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProduceAsync(Stream payloadStream, string key, CancellationToken cancellationToken = default);
    }
}
