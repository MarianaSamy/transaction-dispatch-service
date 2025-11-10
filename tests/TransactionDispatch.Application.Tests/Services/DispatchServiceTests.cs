using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TransactionDispatch.Application.DTOs;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Application.Services;
using TransactionDispatch.Domain.Models;

namespace TransactionDispatch.Application.Tests.Services
{
    public class DispatchServiceTests
    {
        private static DispatchService CreateSut(
            Mock<IFileProvider>? files = null,
            Mock<IJobRepository>? repo = null,
            Mock<IBackgroundTaskQueue>? queue = null)
        {
            files ??= new Mock<IFileProvider>();
            repo ??= new Mock<IJobRepository>();
            queue ??= new Mock<IBackgroundTaskQueue>();
            var logger = new Mock<ILogger<DispatchService>>();
            return new DispatchService(files.Object, repo.Object, queue.Object, logger.Object);
        }

        private static async IAsyncEnumerable<string> FileList(params string[] paths)
        {
            foreach (var p in paths)
            {
                yield return p;
                await Task.Yield();
            }
        }

        [Fact]
        public async Task StartDispatchAsync_ValidRequest_AddsAndEnqueuesJob_ReturnsJobId()
        {
            // Arrange
            var dto = new DispatchJobRequestDto { FolderPath = "C:/data/inbox", DeleteAfterSend = true };
            var files = new Mock<IFileProvider>();
            files.Setup(f => f.EnumerateFilesAsync(dto.FolderPath, null, It.IsAny<CancellationToken>()))
                 .Returns((string f, IReadOnlyCollection<string>? e, CancellationToken c) => FileList("a.xml", "b.xml"));

            var repo = new Mock<IJobRepository>();
            var queue = new Mock<IBackgroundTaskQueue>();
            var sut = CreateSut(files, repo, queue);

            // Act
            var jobId = await sut.StartDispatchAsync(dto);

            // Assert
            Assert.NotEqual(Guid.Empty, jobId);
            repo.Verify(r => r.AddAsync(It.Is<DispatchJob>(j => j.FolderPath == dto.FolderPath && j.TotalFiles == 2), It.IsAny<CancellationToken>()), Times.Once);
            queue.Verify(q => q.EnqueueJobAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartDispatchAsync_WhenFolderEmpty_ThrowsArgumentException()
        {
            var sut = CreateSut();
            var dto = new DispatchJobRequestDto { FolderPath = "  " };
            await Assert.ThrowsAsync<ArgumentException>(() => sut.StartDispatchAsync(dto));
        }

        [Fact]
        public async Task GetStatusAsync_WhenJobFound_ReturnsCorrectProgress()
        {
            // Arrange
            var job = new DispatchJob("C:/data", totalFiles: 10) { Processed = 5, Successful = 4, Failed = 1 };
            var repo = new Mock<IJobRepository>();
            repo.Setup(r => r.GetByIdAsync(job.JobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);

            var sut = CreateSut(repo: repo);

            // Act
            var result = await sut.GetStatusAsync(job.JobId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("50%", result!.Progress);
            Assert.Equal(10, result.TotalFiles);
            Assert.Equal(5, result.Processed);
        }

        [Fact]
        public async Task GetStatusAsync_WhenJobNotFound_ReturnsNull()
        {
            var repo = new Mock<IJobRepository>();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DispatchJob?)null);
            var sut = CreateSut(repo: repo);

            var result = await sut.GetStatusAsync(Guid.NewGuid());

            Assert.Null(result);
        }
    }
}
