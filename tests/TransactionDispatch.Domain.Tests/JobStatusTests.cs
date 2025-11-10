using System;
using TransactionDispatch.Domain.Models;
using Xunit;

namespace TransactionDispatch.Domain.Tests.Models
{
    public class JobStatusTests
    {
        [Fact]
        public void Constructor_Sets_All_Properties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var folder = "C:/data/inbox";
            var start = new DateTime(2025, 11, 10, 9, 0, 0, DateTimeKind.Utc);
            var end = start.AddMinutes(5);

            // Act
            var status = new JobStatus(
                JobId: id,
                FolderPath: folder,
                TotalFiles: 10,
                Processed: 8,
                Successful: 7,
                Failed: 1,
                ProgressPercentage: 80.0,
                StartedAt: start,
                CompletedAt: end
            );

            // Assert
            Assert.Equal(id, status.JobId);
            Assert.Equal(folder, status.FolderPath);
            Assert.Equal(10, status.TotalFiles);
            Assert.Equal(8, status.Processed);
            Assert.Equal(7, status.Successful);
            Assert.Equal(1, status.Failed);
            Assert.Equal(80.0, status.ProgressPercentage);
            Assert.Equal(start, status.StartedAt);
            Assert.Equal(end, status.CompletedAt);
        }

        [Fact]
        public void Records_WithSameValues_AreEqual()
        {
            // Arrange
            var id = Guid.NewGuid();
            var start = DateTime.UtcNow;

            var s1 = new JobStatus(id, "A", 10, 5, 4, 1, 50.0, start, null);
            var s2 = new JobStatus(id, "A", 10, 5, 4, 1, 50.0, start, null);

            // Assert
            Assert.Equal(s1, s2);
            Assert.True(s1 == s2);
            Assert.False(s1 != s2);
        }

        [Fact]
        public void WithExpression_CreatesModifiedCopy()
        {
            // Arrange
            var baseStatus = new JobStatus(Guid.NewGuid(), "X", 10, 5, 5, 0, 50.0, DateTime.UtcNow, null);

            // Act
            var modified = baseStatus with { Processed = 10, ProgressPercentage = 100.0 };

            // Assert
            Assert.Equal(baseStatus.JobId, modified.JobId);
            Assert.Equal(10, modified.Processed);
            Assert.Equal(100.0, modified.ProgressPercentage);
            Assert.NotEqual(baseStatus, modified); // record equality should differ
        }
    }
}
