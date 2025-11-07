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
            Assert.Equal(ProcessingOutcome.Unknown, file.Outcome);
            Assert.Equal(0, file.Attempts);
            Assert.False(file.IsProcessed);
        }

        [Fact]
        public void Constructor_InvalidPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new DispatchFile(null!));
            Assert.Throws<ArgumentException>(() => new DispatchFile(string.Empty));
            Assert.Throws<ArgumentException>(() => new DispatchFile("   "));
        }

        [Fact]
        public void MarkProcessed_SetsOutcomeAndIncrementsAttempts_AndSetsErrorMessage()
        {
            var file = new DispatchFile(@"C:\inbound\a.xml");

            file.MarkProcessed(ProcessingOutcome.Success);

            Assert.True(file.IsProcessed);
            Assert.Equal(ProcessingOutcome.Success, file.Outcome);
            Assert.Equal(1, file.Attempts);
            Assert.Null(file.ErrorMessage);

            // mark again as failure (simulating another finalization) increments attempts
            file.MarkProcessed(ProcessingOutcome.Failure, "send-failed");
            Assert.Equal(ProcessingOutcome.Failure, file.Outcome);
            Assert.Equal(2, file.Attempts);
            Assert.Equal("send-failed", file.ErrorMessage);
        }

        [Fact]
        public void MarkProcessed_WithUnknownOutcome_ThrowsArgumentException()
        {
            var file = new DispatchFile(@"C:\inbound\a.xml");
            Assert.Throws<ArgumentException>(() => file.MarkProcessed(ProcessingOutcome.Unknown));
        }
    }
}
