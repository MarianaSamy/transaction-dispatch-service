using System;
using System.Collections.Generic;
using System.Linq;
using TransactionDispatch.Domain.Enums;

namespace TransactionDispatch.Domain.Models
{
    /// <summary>
    /// Aggregate root for a dispatch job.
    /// Owns the files discovered at job creation and tracks their processing status.
    /// </summary>
    public class DispatchJob
    {
        public DispatchJob(string folderPath, IEnumerable<DispatchFile> files)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Folder path must not be empty", nameof(folderPath));

            if (files == null)
                throw new ArgumentNullException(nameof(files));

            JobId = Guid.NewGuid();
            FolderPath = folderPath;
            StartedAt = DateTime.UtcNow;
            _files = new List<DispatchFile>(files);
        }

        // Identity and meta
        public Guid JobId { get; }
        public string FolderPath { get; }
        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; private set; }

        // Internal file collection
        private readonly List<DispatchFile> _files;
        public IReadOnlyList<DispatchFile> Files => _files.AsReadOnly();

        // Derived counts
        public int TotalFiles => _files.Count;
        public int Processed => _files.Count(f => f.IsProcessed);
        public int Successful => _files.Count(f => f.Outcome == ProcessingOutcome.Success);
        public int Failed => _files.Count(f => f.Outcome == ProcessingOutcome.Failure);

        public double ProgressPercentage =>
            TotalFiles == 0 ? 100.0 : Math.Round((Processed / (double)TotalFiles) * 100.0, 2);

        public bool IsCompleted => CompletedAt.HasValue;

        /// <summary>
        /// Marks a file as processed by its path.
        /// </summary>
        public void MarkFileProcessed(string path, ProcessingOutcome outcome, string? errorMessage = null)
        {
            var file = _files.FirstOrDefault(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));
            if (file == null)
                throw new InvalidOperationException($"File '{path}' not found in this job.");

            file.MarkProcessed(outcome, errorMessage);

            if (Processed >= TotalFiles && !IsCompleted)
                MarkCompleted();
        }

        private void MarkCompleted()
        {
            if (!CompletedAt.HasValue)
                CompletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns an immutable snapshot of this job's status.
        /// </summary>
        public JobStatus ToStatus() =>
            new JobStatus(JobId, FolderPath, TotalFiles, Processed, Successful, Failed,
                          ProgressPercentage, StartedAt, CompletedAt);
    }
}
