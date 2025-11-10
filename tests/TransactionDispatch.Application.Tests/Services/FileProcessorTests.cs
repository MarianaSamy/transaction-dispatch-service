using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Application.Options;
using TransactionDispatch.Domain.Enums;
using TransactionDispatch.Domain.Models;
using TransactionDispatch.Infrastructure.Services;
using Xunit;

namespace TransactionDispatch.Infrastructure.Tests.Services
{
    public class FileProcessorTests
    {
        // Very small IOptions<T> shim to avoid Options.Create dependency
        private class FakeOptions<T> : Microsoft.Extensions.Options.IOptions<T> where T : class, new()
        {
            public FakeOptions(T value) => Value = value;
            public T Value { get; }
        }

        private static (FileProcessor sut, Mock<IFileProvider> files, Mock<IKafkaProducer> producer, Mock<IJobRepository> repo)
            CreateSut(int maxRetries = 1)
        {
            var files = new Mock<IFileProvider>();
            var producer = new Mock<IKafkaProducer>();
            var repo = new Mock<IJobRepository>();
            var options = new FakeOptions<DispatchOptions>(new DispatchOptions { MaxRetries = maxRetries, RetryDelayMs = 1 });
            var logger = new Mock<ILogger<TransactionDispatch.Infrastructure.Services.FileProcessor>>().Object;

            var sut = new TransactionDispatch.Infrastructure.Services.FileProcessor(
                files.Object, producer.Object, repo.Object, options, logger);

            return (sut, files, producer, repo);
        }

        [Fact]
        public async Task ProcessFileAsync_Success_ReturnsTrueAndUpdatesJob()
        {
            // Arrange
            var filePath = @"C:\inbound\a.xml";
            var job = new DispatchJob(@"C:\inbound", totalFiles: 1);
            var (sut, files, producer, repo) = CreateSut();

            repo.Setup(r => r.GetByIdAsync(job.JobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            files.Setup(f => f.IsFileAvailableAsync(filePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            files.Setup(f => f.OpenReadAsync(filePath, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream(new byte[] { 1 }));
            producer.Setup(p => p.ProduceAsync(It.IsAny<Stream>(), "a.xml", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            files.Setup(f => f.DeleteAsync(filePath, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await sut.ProcessFileAsync(filePath, job.JobId, deleteAfterSend: true, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Equal(1, job.Processed);
            Assert.Equal(1, job.Successful);
            Assert.Equal(JobStatusEnum.Completed, job.Status);
        }

        [Fact]
        public async Task ProcessFileAsync_FileNotAvailable_ReturnsFalseAndMarksFailed()
        {
            // Arrange
            var filePath = @"C:\inbound\missing.xml";
            var job = new DispatchJob(@"C:\inbound");
            var (sut, files, _, repo) = CreateSut();

            repo.Setup(r => r.GetByIdAsync(job.JobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            files.Setup(f => f.IsFileAvailableAsync(filePath, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await sut.ProcessFileAsync(filePath, job.JobId, deleteAfterSend: false, CancellationToken.None);

            // Assert
            Assert.False(result);
            Assert.Equal(1, job.Processed);
            Assert.Equal(1, job.Failed);
        }

        [Fact]
        public async Task ProcessFileAsync_ProducerFails_RespectsRetriesThenFails()
        {
            // Arrange
            var filePath = @"C:\inbound\bad.xml";
            var job = new DispatchJob(@"C:\inbound");
            var (sut, files, producer, repo) = CreateSut(maxRetries: 1);

            repo.Setup(r => r.GetByIdAsync(job.JobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            files.Setup(f => f.IsFileAvailableAsync(filePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            files.Setup(f => f.OpenReadAsync(filePath, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream(new byte[] { 9 }));
            producer.Setup(p => p.ProduceAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("kafka"));

            // Act
            var result = await sut.ProcessFileAsync(filePath, job.JobId, deleteAfterSend: false, CancellationToken.None);

            // Assert
            Assert.False(result);
            Assert.Equal(1, job.Processed);
            Assert.Equal(1, job.Failed);
            Assert.Equal(JobStatusEnum.Failed, job.Status);
        }
    }
}
