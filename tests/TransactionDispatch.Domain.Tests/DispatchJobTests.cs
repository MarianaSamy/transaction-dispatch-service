using System;
using Xunit;
using TransactionDispatch.Domain.Models;
using TransactionDispatch.Domain.Enums;

namespace TransactionDispatch.Domain.Tests
{
    public class DispatchJobTests
    {
        [Fact]
        public void Constructor_InitializesProperties_WhenValidInput()
        {
            // Arrange
            var folder = "C:/data/inbox";
            long totalFiles = 10;
            bool deleteAfterSend = true;

            // Act
            var job = new DispatchJob(folder, totalFiles, deleteAfterSend);

            // Assert
            Assert.NotEqual(Guid.Empty, job.JobId);
            Assert.Equal(folder, job.FolderPath);
            // StartedAt should be set to a recent time (within a reasonable range)
            var now = DateTime.UtcNow;
            var lowerBound = now.AddSeconds(-5);
            var upperBound = now.AddSeconds(5);
            Assert.InRange(job.StartedAt, lowerBound, upperBound);

            Assert.Equal(totalFiles, job.TotalFiles);
            Assert.Equal(deleteAfterSend, job.DeleteAfterSend);

            // default counters
            Assert.Equal(0, job.Processed);
            Assert.Equal(0, job.Successful);
            Assert.Equal(0, job.Failed);

            Assert.Equal(JobStatusEnum.Pending, job.Status);
            Assert.Null(job.LastError);

            Assert.NotNull(job.Files);
            Assert.Empty(job.Files);
            Assert.Null(job.CompletedAt);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ThrowsArgumentException_WhenFolderPathIsNullOrWhiteSpace(string folderPath)
        {
            Assert.Throws<ArgumentException>(() => new DispatchJob(folderPath!));
        }

        [Fact]
        public void MarkAsCompleted_SetsStatusAndCompletedAt()
        {
            // Arrange
            var job = new DispatchJob("C:/some/path");

            // Act
            job.MarkAsCompleted();

            // Assert
            Assert.Equal(JobStatusEnum.Completed, job.Status);
            Assert.NotNull(job.CompletedAt);

            var now = DateTime.UtcNow;
            var lowerBound = now.AddSeconds(-5);
            var upperBound = now.AddSeconds(5);
            Assert.InRange(job.CompletedAt.Value, lowerBound, upperBound);
        }

        [Fact]
        public void MarkAsFailed_SetsStatusAndLastError()
        {
            // Arrange
            var job = new DispatchJob("C:/some/path");
            const string error = "Something went wrong";

            // Act
            job.MarkAsFailed(error);

            // Assert
            Assert.Equal(JobStatusEnum.Failed, job.Status);
            Assert.Equal(error, job.LastError);
        }

        [Fact]
        public void Counters_AreMutable()
        {
            // Arrange
            var job = new DispatchJob("C:/some/path");

            // Act
            job.Processed = 5;
            job.Successful = 4;
            job.Failed = 1;

            // Assert
            Assert.Equal(5, job.Processed);
            Assert.Equal(4, job.Successful);
            Assert.Equal(1, job.Failed);
        }

        [Fact]
        public void Files_IsReadOnlyCollection()
        {
            // Arrange
            var job = new DispatchJob("C:/some/path");

            // Assert that the exposed Files collection is read-only and initially empty
            var files = job.Files;
            Assert.NotNull(files);
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<Domain.Models.DispatchFile>>(files);
            Assert.Empty(files);
        }
    }
}
