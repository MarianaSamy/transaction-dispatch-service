using System;

namespace TransactionDispatch.Application.DTOs
{
    public class DispatchJobStatusDto
    {
        public Guid JobId { get; set; }
        public string FolderPath { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int Processed { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public double ProgressPercentage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty; // Running, Completed, Failed
    }
}
