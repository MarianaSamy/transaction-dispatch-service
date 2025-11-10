using System;

namespace TransactionDispatch.Application.DTOs
{
    public class JobStatusDto
    {
        public JobStatusDto(string Progress, int TotalFiles, int Processed, int Successful, int Failed)
        {
            this.Progress = Progress;
            this.TotalFiles = TotalFiles;
            this.Processed = Processed;
            this.Successful = Successful;
            this.Failed = Failed;
        }

        public Guid JobId { get; set; }
        public string FolderPath { get; set; } = string.Empty;
        public long TotalFiles { get; set; }
        public long Processed { get; set; }
        public long Successful { get; set; }
        public long Failed { get; set; }
        public string Progress { get; set; }= string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
