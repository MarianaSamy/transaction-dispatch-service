// JobProcessor.cs (Application.Services)
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Application.Options;
using TransactionDispatch.Domain.Models;

public class JobProcessor : IJobProcessor
{
    private readonly IJobRepository _jobRepository;
    private readonly IFileProvider _fileProvider;
    private readonly IFileProcessor _fileProcessor;
    private readonly ILogger<JobProcessor> _logger;
    private readonly DispatchOptions _options;

    public JobProcessor(IJobRepository jobRepository, IFileProvider fileProvider, IFileProcessor fileProcessor,
                        IOptions<DispatchOptions> options, ILogger<JobProcessor> logger)
    {
        _jobRepository = jobRepository;
        _fileProvider = fileProvider;
        _fileProcessor = fileProcessor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job == null) { _logger.LogWarning("Job {JobId} not found", jobId); return; }

        bool deleteAfterSend = TryGetDeleteAfterSend(job);

        var semaphore = new SemaphoreSlim(Math.Max(1, _options.MaxParallelism));
        var running = new List<Task>();

        await foreach (var filePath in _fileProvider.EnumerateFilesAsync(job.FolderPath, null, cancellationToken))
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            var task = ProcessFileWrapperAsync(filePath, job, deleteAfterSend, semaphore, cancellationToken);
            running.Add(task);

            if (running.Count >= _options.MaxParallelism)
            {
                var finished = await Task.WhenAny(running).ConfigureAwait(false);
                running.RemoveAll(t => t.IsCompleted);
            }
        }

        if (running.Any())
            await Task.WhenAll(running).ConfigureAwait(false);

        await _jobRepository.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessFileWrapperAsync(string filePath, DispatchJob job, bool deleteAfterSend, SemaphoreSlim semaphore, CancellationToken ct)
    {
        try
        {
            var success = await _fileProcessor.ProcessFileAsync(filePath, job.JobId, deleteAfterSend, ct).ConfigureAwait(false);           
        }
        finally
        {
            try { semaphore.Release(); } catch { }
        }
    }

    private static bool TryGetDeleteAfterSend(DispatchJob job) { /* same helper as before */ return false; }
}
