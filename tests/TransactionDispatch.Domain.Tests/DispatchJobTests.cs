using System;
using System.Linq;
using Xunit;
using TransactionDispatch.Domain.Models;
using TransactionDispatch.Domain.Enums;
using System.Collections.Generic;

namespace TransactionDispatch.Domain.Tests
{
    public class DispatchJobTests
    {
        [Fact]
        public void Constructor_SetsInitialValues()
        {
            var files = new List<DispatchFile>();
            var folder = @"C:\inbound";
            var job = new DispatchJob(folder, files);

            Assert.NotEqual(Guid.Empty, job.JobId);
            Assert.Equal(folder, job.FolderPath);
            Assert.Equal(0, job.TotalFiles);
            Assert.Equal(0, job.Processed);
            Assert.Equal(0, job.Successful);
            Assert.Equal(0, job.Failed);
            Assert.False(job.IsCompleted);
            Assert.Equal(100.0, job.ProgressPercentage);
        }

        [Fact]
        public void MarkFileProcessed_UpdatesCounts_And_MarksCompletedWhenAllProcessed()
        {
            var files = new[]
            {
                new DispatchFile(@"C:\inbound\a.xml"),
                new DispatchFile(@"C:\inbound\b.xml")
            };

            var job = new DispatchJob(@"C:\inbound", files);

            job.MarkFileProcessed(@"C:\inbound\a.xml", ProcessingOutcome.Success);
            Assert.Equal(1, job.Processed);
            Assert.Equal(1, job.Successful);
            Assert.False(job.IsCompleted);

            job.MarkFileProcessed(@"C:\inbound\b.xml", ProcessingOutcome.Failure);
            Assert.Equal(2, job.Processed);
            Assert.Equal(1, job.Successful);
            Assert.Equal(1, job.Failed);
            Assert.True(job.IsCompleted);
            Assert.NotNull(job.CompletedAt);
        }

        [Fact]
        public void MarkFileProcessed_ThrowsIfFileNotFound()
        {
            var files = new[] { new DispatchFile(@"C:\inbound\a.xml") };
            var job = new DispatchJob(@"C:\inbound", files);

            Assert.Throws<InvalidOperationException>(() =>
                job.MarkFileProcessed(@"C:\inbound\missing.xml", ProcessingOutcome.Success));
        }

        [Fact]
        public void ToStatus_ProducesCorrectSnapshot()
        {
            var files = new[]
            {
                new DispatchFile(@"C:\inbound\a.xml"),
                new DispatchFile(@"C:\inbound\b.xml")
            };
            var job = new DispatchJob(@"C:\inbound", files);

            job.MarkFileProcessed(@"C:\inbound\a.xml", ProcessingOutcome.Success);
            var status = job.ToStatus();

            Assert.Equal(job.JobId, status.JobId);
            Assert.Equal(job.FolderPath, status.FolderPath);
            Assert.Equal(job.TotalFiles, status.TotalFiles);
            Assert.Equal(job.Processed, status.Processed);
            Assert.Equal(job.Successful, status.Successful);
            Assert.Equal(job.Failed, status.Failed);
            Assert.Equal(job.ProgressPercentage, status.ProgressPercentage);
        }
    }
}
