using System;

namespace TransactionDispatch.Domain.Models
{
    /// <summary>
    /// Lightweight immutable snapshot of a DispatchJob's status.
    /// Produced from DispatchJob.ToStatus().
    /// </summary>
    public sealed record JobStatus(
        Guid JobId,
        string FolderPath,
        int TotalFiles,
        int Processed,
        int Successful,
        int Failed,
        double ProgressPercentage,
        DateTime StartedAt,
        DateTime? CompletedAt
    );
}
