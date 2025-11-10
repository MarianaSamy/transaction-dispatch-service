using System;
using System.Collections.Generic;
using TransactionDispatch.Domain.Enums;

namespace TransactionDispatch.Domain.Models
{
    public class DispatchJob
    {
        // EF requires a parameterless constructor
        protected DispatchJob()
        {
            JobId = Guid.NewGuid();
            StartedAt = DateTime.UtcNow;
            _files = new List<DispatchFile>();
        }

        public DispatchJob(string folderPath, long totalFiles = 0, bool deleteAfterSend = false)
            : this()
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("FolderPath must not be empty", nameof(folderPath));

            FolderPath = folderPath;
            TotalFiles = totalFiles;
            DeleteAfterSend = deleteAfterSend;
            Status = JobStatusEnum.Pending;
        }

        public Guid JobId { get; private set; }
        public string FolderPath { get; private set; } = string.Empty;
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get;  set; }

        public long TotalFiles { get; set; }
        public bool DeleteAfterSend { get; set; }

        // These are now regular persisted properties (with setters)
        public long Processed { get; set; }
        public long Successful { get; set; }
        public long Failed { get; set; }

        public JobStatusEnum Status { get; set; }
        public string? LastError { get; set; }

        private readonly List<DispatchFile> _files;
        public IReadOnlyList<DispatchFile> Files => _files.AsReadOnly();

        public void MarkAsCompleted()
        {
            Status = JobStatusEnum.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void MarkAsFailed(string? error = null)
        {
            Status = JobStatusEnum.Failed;
            LastError = error;
        }
    }
}
