using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionDispatch.Application.Interfaces
{
    public interface IKafkaProducer
    {
        /// <summary>
        /// Sends the contents of a stream (representing a single file or message) to a Kafka topic.
        /// </summary>
        /// <param name="stream">The stream containing the file or message data.</param>
        /// <param name="fileName">The original file name or identifier for tracing.</param>
        /// <param name="cancellationToken">Cancellation token for async control.</param>
        Task ProduceAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    }
}
