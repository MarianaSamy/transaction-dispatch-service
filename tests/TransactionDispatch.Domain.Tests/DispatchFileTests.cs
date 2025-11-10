using System;
using Xunit;
using TransactionDispatch.Domain.Models;
using TransactionDispatch.Domain.Enums;

namespace TransactionDispatch.Domain.Tests
{
    public class DispatchFileTests
    {
        [Fact]
        public void Constructor_WithValidPath_SetsProperties()
        {
            // Arrange
            var path = @"C:\inbound\orders-20251107.xml";
            long length = 12345;
            var created = new DateTime(2025, 11, 7, 10, 30, 0, DateTimeKind.Utc);

            // Act
            var file = new DispatchFile(path, length, created);

            // Assert
            Assert.Equal(path, file.Path);
            Assert.Equal("orders-20251107.xml", file.FileName);
            Assert.Equal(length, file.Length);
            Assert.Equal(created, file.CreatedUtc);
            Assert.Equal(ProcessingOutcomeEnum.Unknown, file.Outcome);
            Assert.Equal(0, file.Attempts);
            Assert.False(file.IsProcessed);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidPath_ThrowsArgumentException(string? path)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new DispatchFile(path!));
            Assert.Contains("Path", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MarkProcessed_SetsOutcomeAndIncrementsAttempts_AndSetsErrorMessage()
        {
            // Arrange
            var file = new DispatchFile(@"C:\inbound\a.xml");

            // Act
            file.MarkProcessed(ProcessingOutcomeEnum.Success);

            // Assert first mark
            Assert.True(file.IsProcessed);
            Assert.Equal(ProcessingOutcomeEnum.Success, file.Outcome);
            Assert.Equal(1, file.Attempts);
            Assert.Null(file.ErrorMessage);

            // Act again to simulate retry
            file.MarkProcessed(ProcessingOutcomeEnum.Failure, "send-failed");

            // Assert second mark
            Assert.True(file.IsProcessed);
            Assert.Equal(ProcessingOutcomeEnum.Failure, file.Outcome);
            Assert.Equal(2, file.Attempts);
            Assert.Equal("send-failed", file.ErrorMessage);
        }

        [Fact]
        public void MarkProcessed_WithUnknownOutcome_ThrowsArgumentException()
        {
            // Arrange
            var file = new DispatchFile(@"C:\inbound\a.xml");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => file.MarkProcessed(ProcessingOutcomeEnum.Unknown));
            Assert.Contains("Unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MarkProcessed_SetsProcessedFlagRegardlessOfOutcome()
        {
            // Arrange
            var file = new DispatchFile(@"C:\inbound\b.xml");

            // Act
            file.MarkProcessed(ProcessingOutcomeEnum.Failure, "network error");

            // Assert
            Assert.True(file.IsProcessed);
            Assert.Equal("network error", file.ErrorMessage);
            Assert.Equal(ProcessingOutcomeEnum.Failure, file.Outcome);
            Assert.Equal(1, file.Attempts);
        }
    }
}
