using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TransactionDispatch.Application.Options;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Domain.Models;

namespace TransactionDispatch.Application.Tests.Services
{
    public class JobProcessorTests
    {
        // Simple FakeOptions to avoid Options.Create dependency
        private class FakeOptions<T> : Microsoft.Extensions.Options.IOptions<T> where T : class, new()
        {
            public FakeOptions(T value) => Value = value;
            public T Value { get; }
        }

        private static DispatchJob CreateJob(string folder = @"C:\inbound")
            => new DispatchJob(folder);

        // Helper to produce IAsyncEnumerable<string> for files
        private static async IAsyncEnumerable<string> FilePaths(params string[] paths)
        {
            foreach (var p in paths)
            {
                yield return p;
                await Task.Yield();
            }
        }

        [Fact]
        public async Task ProcessJobAsync_WhenJobNotFound_DoesNotCallUpdateOrProcessFiles()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var repoMock = new Mock<IJobRepository>();
            repoMock
                .Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DispatchJob?)null);

            var fileProviderMock = new Mock<IFileProvider>();
            var fileProcessorMock = new Mock<IFileProcessor>();
            var options = new FakeOptions<DispatchOptions>(new DispatchOptions { MaxParallelism = 2 });
            var logger = new Mock<ILogger<JobProcessor>>();

            var processor = new JobProcessor(
                repoMock.Object,
                fileProviderMock.Object,
                fileProcessorMock.Object,
                options,
                logger.Object);

            // Act
            await processor.ProcessJobAsync(jobId);

            // Assert
            repoMock.Verify(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.UpdateAsync(It.IsAny<DispatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
            fileProviderMock.Verify(fp => fp.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
            fileProcessorMock.Verify(fp => fp.ProcessFileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessJobAsync_WithFiles_CallsProcessorForEachFile_AndUpdatesRepo()
        {
            // Arrange
            var job = CreateJob();
            var jobId = job.JobId;
            var files = new[] { @"C:\inbound\a.xml", @"C:\inbound\b.xml" };

            var repoMock = new Mock<IJobRepository>();
            repoMock
                .Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            repoMock
                .Setup(r => r.UpdateAsync(It.IsAny<DispatchJob>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var fileProviderMock = new Mock<IFileProvider>();
            fileProviderMock
                .Setup(fp => fp.EnumerateFilesAsync(job.FolderPath, null, It.IsAny<CancellationToken>()))
                .Returns((string folder, IReadOnlyCollection<string>? ext, CancellationToken ct) => FilePaths(files));

            var fileProcessorMock = new Mock<IFileProcessor>();
            // Simulate processing returning true for every file
            fileProcessorMock
                .Setup(fp => fp.ProcessFileAsync(It.IsAny<string>(), jobId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var options = new FakeOptions<DispatchOptions>(new DispatchOptions { MaxParallelism = 2 });
            var logger = new Mock<ILogger<JobProcessor>>();

            var processor = new JobProcessor(
                repoMock.Object,
                fileProviderMock.Object,
                fileProcessorMock.Object,
                options,
                logger.Object);

            // Act
            await processor.ProcessJobAsync(jobId);

            // Assert basic flow
            repoMock.Verify(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.UpdateAsync(It.Is<DispatchJob>(j => j.JobId == jobId), It.IsAny<CancellationToken>()), Times.Once);

            fileProviderMock.Verify(fp => fp.EnumerateFilesAsync(job.FolderPath, null, It.IsAny<CancellationToken>()), Times.Once);

            // Verify processor was called for each file path at least once
            foreach (var f in files)
            {
                fileProcessorMock.Verify(fp => fp.ProcessFileAsync(f, jobId, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            }

            // Optionally assert job counters updated if your implementation increments them:
            // Assert.Equal(files.Length, job.Processed);
            // Assert.Equal(files.Length, job.Successful);
        }
    }
}
