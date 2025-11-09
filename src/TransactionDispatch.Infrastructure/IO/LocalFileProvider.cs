// TransactionDispatch.Infrastructure.IO.LocalFileProvider.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Application.Options;

namespace TransactionDispatch.Infrastructure.IO
{
    /// <summary>
    /// Provides access to local files for enumeration, validation, and reading.
    /// Designed for small transactional files (~20 KB average).
    /// </summary>
    public class LocalFileProvider : IFileProvider
    {
        private readonly HashSet<string> _allowedExtensions;
        private readonly long _maxFileSizeBytes;

        public LocalFileProvider(
            IOptions<AllowedFileTypesOptions> fileTypes,
            IOptions<FileProviderOptions> settings)
        {
            var configuredTypes = fileTypes?.Value?.AllowedFileTypes ?? new[] { "xml" };

            _allowedExtensions = new HashSet<string>(
                configuredTypes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().TrimStart('.').ToLowerInvariant())
            );

            _maxFileSizeBytes = settings?.Value?.MaxFileSizeBytes ?? 200 * 1024; // default 200 KB
        }

        public async IAsyncEnumerable<string> EnumerateFilesAsync(
            string folderPath,
            IReadOnlyCollection<string>? allowedExtensions = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("folderPath is required.", nameof(folderPath));

            var allowed = allowedExtensions?.Any() == true
                ? new HashSet<string>(allowedExtensions.Select(x => x.Trim().TrimStart('.').ToLowerInvariant()))
                : _allowedExtensions;

            foreach (var filePath in Directory.EnumerateFiles(folderPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsValidFile(filePath, allowed))
                    yield return filePath;
            }

            await Task.CompletedTask;
        }

        private bool IsValidFile(string filePath, HashSet<string> allowedExtensions)
        {
            try
            {
                var extension = Path.GetExtension(filePath)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
                if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(extension))
                    return false;

                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists || fileInfo.Length <= 0 || fileInfo.Length > _maxFileSizeBytes)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path is required.", nameof(path));

            Stream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path is required.", nameof(path));

            File.Delete(path);
            return Task.CompletedTask;
        }

        public Task<bool> IsFileAvailableAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path is required.", nameof(path));

            if (!File.Exists(path))
                return Task.FromResult(false);

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 1, FileOptions.None);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
