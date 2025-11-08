using System;

namespace TransactionDispatch.Application.DTOs
{
    public class JobStatusDto
    {
        public Guid JobId { get; set; }
        public string FolderPath { get; set; } = string.Empty;
        public long TotalFiles { get; set; }
        public long Processed { get; set; }
        public long Successful { get; set; }
        public long Failed { get; set; }
        public double ProgressPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
