using System;
using Xunit;
using TransactionDispatch.Domain.Models;
using TransactionDispatch.Domain.Enums;

namespace TransactionDispatch.Domain.Tests
{
    public class JobStatusTests
    {
        [Fact]
        public void ToStatus_ProducesCorrectSnapshot_FromDispatchJob()
        {
            // Arrange: create job with two files
            var files = new[]
            {
                new DispatchFile(@"C:\inbound\a.xml"),
                new DispatchFile(@"C:\inbound\b.xml")
            };
            var job = new DispatchJob(@"C:\inbound", files);

            // Act: mark one success, one failure
            job.MarkFileProcessed(@"C:\inbound\a.xml", ProcessingOutcome.Success);
            job.MarkFileProcessed(@"C:\inbound\b.xml", ProcessingOutcome.Failure);
            var status = job.ToStatus();

            // Assert snapshot values
            Assert.Equal(job.JobId, status.JobId);
            Assert.Equal(job.FolderPath, status.FolderPath);
            Assert.Equal(2, status.TotalFiles);
            Assert.Equal(2, status.Processed);
            Assert.Equal(1, status.Successful);
            Assert.Equal(1, status.Failed);
            Assert.Equal(job.ProgressPercentage, status.ProgressPercentage);
            Assert.Equal(job.StartedAt, status.StartedAt);
            Assert.Equal(job.CompletedAt, status.CompletedAt);
        }

        [Fact]
        public void JobStatus_IsImmutable_RecordKeepsValues()
        {
            var now = DateTime.UtcNow;
            var id = Guid.NewGuid();

            var status = new JobStatus(
                id,
                @"C:\inbound",
                TotalFiles: 3,
                Processed: 1,
                Successful: 1,
                Failed: 0,
                ProgressPercentage: 33.33,
                StartedAt: now,
                CompletedAt: null
            );

            // Assert assigned values preserved
            Assert.Equal(id, status.JobId);
            Assert.Equal(@"C:\inbound", status.FolderPath);
            Assert.Equal(3, status.TotalFiles);
            Assert.Equal(1, status.Processed);
            Assert.Equal(1, status.Successful);
            Assert.Equal(0, status.Failed);
            Assert.Equal(33.33, status.ProgressPercentage);
            Assert.Equal(now, status.StartedAt);
            Assert.Null(status.CompletedAt);
        }
    }
}
