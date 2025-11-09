using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionDispatch.Application.Interfaces
{
    /// <summary>
    /// File system abstraction used by the dispatch service.
    /// Implementations should stream results and avoid materializing large lists in memory.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Enumerate full file paths in the given folder, optionally filtered by allowed extensions.
        /// Implementations should yield paths as they are discovered (IAsyncEnumerable) to support very large folders.
        /// Extensions should be provided without a leading dot (e.g., "xml", "csv").
        /// If allowedExtensions is null or empty, the provider may use a configured default list (injected options).
        /// </summary>
        IAsyncEnumerable<string> EnumerateFilesAsync(
            string folderPath,
            IReadOnlyCollection<string>? allowedExtensions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Open a read-only stream for the given file path. Caller must dispose the returned stream.
        /// Implementations should prefer async streaming reads and avoid loading whole file into memory.
        /// </summary>
        Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete the specified file. Throw on unrecoverable errors so the caller can handle retries.
        /// </summary>
        Task DeleteAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optional check whether the file is currently available for processing (not locked or being written to).
        /// Implementations may return true if no locking checks are needed.
        /// </summary>
        Task<bool> IsFileAvailableAsync(string path, CancellationToken cancellationToken = default);
    }
}
