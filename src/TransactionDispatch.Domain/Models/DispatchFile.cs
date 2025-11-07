using System;
using TransactionDispatch.Domain.Enums;

namespace TransactionDispatch.Domain.Models
{
    /// <summary>
    /// Entity representing a single file that belongs to a DispatchJob.
    /// Tracks per-file processing state and metadata.
    /// </summary>
    public class DispatchFile
    {
        public DispatchFile(string path, long length = 0, DateTime? createdUtc = null)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));

            Id = Guid.NewGuid();
            Path = path;
            FileName = System.IO.Path.GetFileName(path) ?? string.Empty;
            Length = length;
            CreatedUtc = createdUtc ?? DateTime.UtcNow;
            Outcome = ProcessingOutcome.Unknown;
            Attempts = 0;
            ErrorMessage = null;
        }

        // Identity (entity)
        public Guid Id { get; init; }

        // File metadata
        public string Path { get; init; }
        public string FileName { get; init; }
        public long Length { get; init; }
        public DateTime CreatedUtc { get; init; }

        // Processing state
        public ProcessingOutcome Outcome { get; private set; }
        public int Attempts { get; private set; }
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// True when an outcome was recorded (success or failure).
        /// </summary>
        public bool IsProcessed => Outcome != ProcessingOutcome.Unknown;

        /// <summary>
        /// Mark the file as processed (success or failure). This also increments Attempts.
        /// </summary>
        public void MarkProcessed(ProcessingOutcome outcome, string? errorMessage = null)
        {
            if (outcome == ProcessingOutcome.Unknown)
                throw new ArgumentException("Outcome must be Success or Failure.", nameof(outcome));

            Attempts++;
            Outcome = outcome;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Increment attempt counter (used when a send attempt occurs but outcome not yet decided).
        /// </summary>
        public void IncrementAttempt() => Attempts++;
    }
}
