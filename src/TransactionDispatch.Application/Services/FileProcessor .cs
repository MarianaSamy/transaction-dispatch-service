using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Application.Options;
using TransactionDispatch.Domain.Enums;
using TransactionDispatch.Domain.Models;

namespace TransactionDispatch.Infrastructure.Services
{
    public class FileProcessor : IFileProcessor
    {
        private readonly IFileProvider _fileProvider;
        private readonly IKafkaProducer _producer;
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<FileProcessor> _logger;
        private readonly DispatchOptions _options;

        public FileProcessor(
            IFileProvider fileProvider,
            IKafkaProducer producer,
            IJobRepository jobRepository,
            IOptions<DispatchOptions> options,
            ILogger<FileProcessor> logger)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new DispatchOptions();
        }

        /// <summary>
        /// Process a single file: availability check, open stream and produce to Kafka with retries,
        /// update job counters and persist job, delete file if requested.
        /// Returns true on success, false on permanent failure.
        /// </summary>
        public async Task<bool> ProcessFileAsync(string filePath, Guid jobId, bool deleteAfterSend, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            ct.ThrowIfCancellationRequested();

            var job = await _jobRepository.GetByIdAsync(jobId, ct).ConfigureAwait(false);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found while processing file {File}", jobId, filePath);
                return false;
            }

            // Availability check
            try
            {
                var available = await _fileProvider.IsFileAvailableAsync(filePath, ct).ConfigureAwait(false);
                if (!available)
                {
                    _logger.LogWarning("File {FilePath} is not available for job {JobId}. Marking as failed.", filePath, jobId);
                    job.Failed++;
                    job.Processed++;
                    await SafeUpdateJobAsync(job, ct).ConfigureAwait(false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                // don't fail immediately; log and continue to attempt processing
                _logger.LogWarning(ex, "IsFileAvailableAsync threw for {FilePath}; continuing to attempt processing.", filePath);
            }

            int attempt = 0;

            while (true)
            {
                attempt++;
                ct.ThrowIfCancellationRequested();

                try
                {
                    await using var stream = await _fileProvider.OpenReadAsync(filePath, ct).ConfigureAwait(false);

                    // produce to Kafka
                    await _producer.ProduceAsync(stream, Path.GetFileName(filePath) ?? string.Empty, ct).ConfigureAwait(false);

                    // success: update counters
                    job.Successful++;
                    job.Processed++;

                    if (job.TotalFiles > 0 && job.Processed >= job.TotalFiles)
                    {
                        job.CompletedAt = DateTime.UtcNow;
                        job.Status = JobStatusEnum.Completed;
                    }

                    await SafeUpdateJobAsync(job, ct).ConfigureAwait(false);

                    // delete file if requested (best-effort)
                    if (deleteAfterSend)
                    {
                        try
                        {
                            await _fileProvider.DeleteAsync(filePath, ct).ConfigureAwait(false);
                        }
                        catch (Exception delEx)
                        {
                            _logger.LogWarning(delEx, "Failed to delete file {FilePath} after successful produce (job {JobId})", filePath, jobId);
                        }
                    }

                    _logger.LogInformation("Successfully processed file {FilePath} (job {JobId})", filePath, jobId);
                    return true;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed producing file {FilePath} for job {JobId}", attempt, filePath, jobId);

                    if (attempt >= Math.Max(1, _options.MaxRetries))
                    {
                        // permanent failure
                        job.Failed++;
                        job.Processed++;
                        job.Status = JobStatusEnum.Failed;

                        await SafeUpdateJobAsync(job, ct).ConfigureAwait(false);

                        _logger.LogError(ex, "File {FilePath} permanently failed after {Attempts} attempts (job {JobId})", filePath, attempt, jobId);
                        return false;
                    }

                    // wait between retries, honor cancellation
                    try
                    {
                        await Task.Delay(_options.RetryDelayMs, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                }
            }
        }

        private async Task SafeUpdateJobAsync(DispatchJob job, CancellationToken ct)
        {
            try
            {
                await _jobRepository.UpdateAsync(job, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist job {JobId} after file update.", job.JobId);
            }
        }
    }
}
