using System;
using System.Collections.Generic;
using System.Linq;
using TransactionDispatch.Domain.Enums;

namespace TransactionDispatch.Domain.Models
{
    public class DispatchJob
    {
        // Constructors
        protected DispatchJob()
        {
            JobId = Guid.NewGuid();
            FolderPath = string.Empty;
            StartedAt = DateTime.UtcNow;
            _files = new List<DispatchFile>();
            Status = JobStatusEnum.InProgress;
        }
        public DispatchJob(string folderPath, IEnumerable<DispatchFile>? files = null)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Folder path must not be empty", nameof(folderPath));

            JobId = Guid.NewGuid();
            FolderPath = folderPath;
            StartedAt = DateTime.UtcNow;
            _files = files != null ? new List<DispatchFile>(files) : new List<DispatchFile>();
            Status = JobStatusEnum.InProgress;
        }

        public DispatchJob(Guid jobId, string folderPath, long totalFiles, long processed,
                           long successful, long failed, DateTime startedAt, DateTime? completedAt,
                           JobStatusEnum status, IEnumerable<DispatchFile>? files = null)
        {
            JobId = jobId;
            FolderPath = folderPath;
            _totalFiles = totalFiles;
            _processed = processed;
            _successful = successful;
            _failed = failed;
            StartedAt = startedAt;
            CompletedAt = completedAt;
            Status = status;
            _files = files != null ? new List<DispatchFile>(files) : new List<DispatchFile>();
        }

        // Properties
        public Guid JobId { get; }
        public string FolderPath { get; }
        public DateTime StartedAt { get; }
        public DateTime? CompletedAt { get; private set; }
        public JobStatusEnum Status { get; private set; }

        private readonly List<DispatchFile> _files;
        public IReadOnlyList<DispatchFile> Files => _files.AsReadOnly();

        private long? _totalFiles;
        public long TotalFiles => _totalFiles ?? _files.Count;

        public void SetTotalFiles(long total) => _totalFiles = total;

        private long _processed;
        private long _successful;
        private long _failed;

        public long Processed => _processed;
        public long Successful => _successful;
        public long Failed => _failed;
        public string? LastError { get; private set; }
        public bool IsCompleted => Status is JobStatusEnum.Completed or JobStatusEnum.Failed or JobStatusEnum.Cancelled;

        // Behavior
        public void MarkFileProcessed(string path, ProcessingOutcomeEnum outcome, string? errorMessage = null)
        {
            if (outcome == ProcessingOutcomeEnum.Unknown)
                throw new ArgumentException("Outcome must be Success or Failure", nameof(outcome));

            _processed++;
            if (outcome == ProcessingOutcomeEnum.Success) _successful++;
            else _failed++;

            if (_processed >= TotalFiles && !IsCompleted)
            {
                CompletedAt = DateTime.UtcNow;
                Status = JobStatusEnum.Completed;
            }

            if (outcome == ProcessingOutcomeEnum.Failure)
                LastError = errorMessage;
        }

        public void MarkFailed(string reason)
        {
            Status = JobStatusEnum.Failed;
            CompletedAt = DateTime.UtcNow;
            LastError = reason;
        }

        public void MarkCancelled(string? reason = null)
        {
            Status = JobStatusEnum.Cancelled;
            CompletedAt = DateTime.UtcNow;
            LastError = reason;
        }

        public void RestoreCounters(long processed, long successful, long failed,
                                    long? totalFiles = null, DateTime? completedAt = null,
                                    JobStatusEnum? status = null, string? lastError = null)
        {
            _processed = processed;
            _successful = successful;
            _failed = failed;
            if (totalFiles.HasValue) _totalFiles = totalFiles.Value;
            if (completedAt.HasValue) CompletedAt = completedAt;
            if (status.HasValue) Status = status.Value;
            LastError = lastError;
        }
    }
}
