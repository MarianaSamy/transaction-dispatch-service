// IFileProcessor.cs (Application.Interfaces)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionDispatch.Application.Interfaces
{
    public interface IFileProcessor
    {
        /// <summary>
        /// Process a single file (produce to Kafka, delete if requested).
        /// Returns true on success, false on permanent failure.
        /// </summary>
        Task<bool> ProcessFileAsync(string filePath, Guid jobId, bool deleteAfterSend, CancellationToken ct = default);
    }
}
