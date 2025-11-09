namespace TransactionDispatch.Application.Options
{
    /// <summary>
    /// Options controlling dispatch concurrency and retry behavior.
    /// Bound from configuration section "Dispatch".
    /// </summary>
    public class DispatchOptions
    {
        /// <summary>
        /// Maximum number of concurrent file producers per job.
        /// </summary>
        public int MaxParallelism { get; set; } = 8;

        /// <summary>
        /// Maximum number of produce retries per file.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay (milliseconds) between retries.
        /// </summary>
        public int RetryDelayMs { get; set; } = 200;
    }
}
