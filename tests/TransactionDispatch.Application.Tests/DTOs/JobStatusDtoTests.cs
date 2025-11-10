using System;
using TransactionDispatch.Application.DTOs;
using Xunit;

namespace TransactionDispatch.Application.Tests.DTOs
{
    public class JobStatusDtoTests
    {
        [Fact]
        public void Constructor_SetsValues_Correctly()
        {
            // Arrange
            var progress = "50%";
            int totalFiles = 10, processed = 5, successful = 4, failed = 1;

            // Act
            var dto = new JobStatusDto(progress, totalFiles, processed, successful, failed);

            // Assert
            Assert.Equal(progress, dto.Progress);
            Assert.Equal(totalFiles, dto.TotalFiles);
            Assert.Equal(processed, dto.Processed);
            Assert.Equal(successful, dto.Successful);
            Assert.Equal(failed, dto.Failed);
        }

        [Fact]
        public void DefaultPropertyValues_AreInitializedCorrectly()
        {
            // Act
            var dto = new JobStatusDto("0%", 0, 0, 0, 0);

            // Assert
            Assert.Equal(Guid.Empty, dto.JobId);
            Assert.Equal(string.Empty, dto.FolderPath);
            Assert.Equal(string.Empty, dto.Status);
            Assert.Equal(default(DateTime), dto.StartedAt);
            Assert.Null(dto.CompletedAt);
        }

        [Fact]
        public void Properties_CanBeMutated()
        {
            // Arrange
            var dto = new JobStatusDto("10%", 5, 2, 2, 1);
            var id = Guid.NewGuid();
            var start = new DateTime(2025, 11, 10, 10, 0, 0, DateTimeKind.Utc);
            var end = start.AddMinutes(2);

            // Act
            dto.JobId = id;
            dto.FolderPath = "C:/jobs/test";
            dto.Status = "Completed";
            dto.StartedAt = start;
            dto.CompletedAt = end;

            // Assert
            Assert.Equal(id, dto.JobId);
            Assert.Equal("C:/jobs/test", dto.FolderPath);
            Assert.Equal("Completed", dto.Status);
            Assert.Equal(start, dto.StartedAt);
            Assert.Equal(end, dto.CompletedAt);
        }
    }
}
