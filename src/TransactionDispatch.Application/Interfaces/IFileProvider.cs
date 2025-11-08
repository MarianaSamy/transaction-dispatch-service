using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionDispatch.Application.Interfaces
{
    /// <summary>
    /// File I/O abstraction. Implementations must stream results and avoid materializing large lists.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Stream file paths from the specified folder, optionally filtered by allowed extensions (without leading dot).
        /// Use streaming enumeration (IAsyncEnumerable) to support very large folders.
        /// </summary>
        /// <param name="folderPath">Root folder to scan.</param>
        /// <param name="allowedExtensions">Extensions without dot (e.g., "xml", "csv"). Null or empty => accept all.</param>
        /// <param name="cancellationToken">Token to cancel enumeration.</param>
        IAsyncEnumerable<string> EnumerateFilesAsync(string folderPath, IReadOnlyCollection<string>? allowedExtensions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Open a read-only stream for the given file path. Caller must dispose the returned stream.
        /// Implementations should prefer async/streaming reads and avoid buffering entire file in memory.
        /// </summary>
        Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete the specified file. Throw on unrecoverable errors so the caller can record and retry.
        /// </summary>
        Task DeleteAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optional helper to check if the file is available for processing (not locked/incomplete).
        /// Implementations may simply return true if no lock-checking is required.
        /// </summary>
        Task<bool> IsFileAvailableAsync(string path, CancellationToken cancellationToken = default);
    }
}
