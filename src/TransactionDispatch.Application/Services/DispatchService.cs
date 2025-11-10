using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TransactionDispatch.Application.DTOs;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Application.Ports;
using TransactionDispatch.Domain.Models;

namespace TransactionDispatch.Application.Services
{
    public class DispatchService : IDispatchService
    {
        private readonly IFileProvider _fileProvider;
        private readonly IJobRepository _jobRepository;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<DispatchService> _logger;

        public DispatchService(
            IFileProvider fileProvider,
            IJobRepository jobRepository,
            IBackgroundTaskQueue taskQueue,
            ILogger<DispatchService> logger)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Start a dispatch job: count files, create/persist job, enqueue for background processing.
        /// Returns JobId immediately.
        /// </summary>
        public async Task<Guid> StartDispatchAsync(DispatchJobRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.FolderPath))
                throw new ArgumentException("FolderPath is required.", nameof(request));

            // Count eligible files (streaming enumeration)
            long totalFiles = 0;
            try
            {
                await foreach (var _ in _fileProvider.EnumerateFilesAsync(request.FolderPath, null, cancellationToken).ConfigureAwait(false))
                    totalFiles++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate files in '{FolderPath}'", request.FolderPath);
                throw;
            }

            // Create DispatchJob using public constructor (no reflection)
            var job = new DispatchJob(request.FolderPath, totalFiles, request.DeleteAfterSend);

            // Persist and enqueue
            await _jobRepository.AddAsync(job, cancellationToken).ConfigureAwait(false);
            await _taskQueue.EnqueueJobAsync(job.JobId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created dispatch job {JobId} for '{FolderPath}' with {TotalFiles} files (deleteAfterSend={Delete})",
                job.JobId, request.FolderPath, totalFiles, request.DeleteAfterSend);

            return job.JobId;
        }

        /// <summary>
        /// Returns snapshot JobStatusDto or null if job not found.
        /// </summary>
        public async Task<JobStatusDto?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            if (jobId == Guid.Empty) throw new ArgumentException("jobId is required.", nameof(jobId));

            var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken).ConfigureAwait(false);
            if (job == null) return null;

            double progress = job.TotalFiles == 0 ? 100.0 : Math.Round((job.Processed / (double)job.TotalFiles) * 100.0, 2);
            var progressText = $"{progress}%";

            return new JobStatusDto(
                Progress: progressText,
                TotalFiles: (int)job.TotalFiles,
                Processed: (int)job.Processed,
                Successful: (int)job.Successful,
                Failed: (int)job.Failed
            );
        }
    }
}
