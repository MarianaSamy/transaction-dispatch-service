using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionDispatch.Application.Options;
using TransactionDispatch.Infrastructure.Kafka;
using Xunit;

namespace TransactionDispatch.Infrastructure.Tests.Kafka
{
    public class ConfluentKafkaProducerTests
    {
        private static ConfluentKafkaProducer CreateWithMockProducer(out Mock<IProducer<string, byte[]>> producerMock, string topic = "t")
        {
            var config = new ProducerConfig(); 
            var options = new KafkaOptions { Topic = topic };
            var logger = new Mock<ILogger<ConfluentKafkaProducer>>().Object;
            var sut = new ConfluentKafkaProducer(config, options, logger);

            // Create mock producer
            producerMock = new Mock<IProducer<string, byte[]>>();

            // Inject the mock into the private field using reflection
            var field = typeof(ConfluentKafkaProducer).GetField("_producer", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new InvalidOperationException("Private field '_producer' not found.");
            field.SetValue(sut, producerMock.Object);

            return sut;
        }

        [Fact]
        public async Task ProduceAsync_NullStream_ThrowsArgumentNullException()
        {
            // Arrange
            var sut = CreateWithMockProducer(out var _);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ProduceAsync(null!, "k"));
        }

        [Fact]
        public async Task ProduceAsync_UnreadableStream_ThrowsArgumentException()
        {
            var sut = CreateWithMockProducer(out var _);

            // create non-readable stream
            var stream = new NonReadableStream();

            await Assert.ThrowsAsync<ArgumentException>(() => sut.ProduceAsync(stream, "k"));
        }

        [Fact]
        public async Task ProduceAsync_WhenDeliveryPersisted_CompletesSuccessfully()
        {
            // Arrange
            var sut = CreateWithMockProducer(out var producerMock);
            var dr = new DeliveryResult<string, byte[]> { Status = PersistenceStatus.Persisted };

            producerMock
                .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dr);

            using var ms = new MemoryStream(new byte[] { 1, 2, 3 });

            // Act
            var ex = await Record.ExceptionAsync(() => sut.ProduceAsync(ms, "key"));

            // Assert
            Assert.Null(ex);
            producerMock.Verify(p => p.ProduceAsync("t", It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProduceAsync_WhenDeliveryNotPersisted_ThrowsException()
        {
            var sut = CreateWithMockProducer(out var producerMock);
            var dr = new DeliveryResult<string, byte[]> { Status = PersistenceStatus.NotPersisted };

            producerMock
                .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dr);

            using var ms = new MemoryStream(new byte[] { 9 });

            await Assert.ThrowsAsync<Exception>(() => sut.ProduceAsync(ms, "k"));
        }

        [Fact]
        public void Dispose_CallsFlushAndDisposeOnProducer()
        {
            var sut = CreateWithMockProducer(out var producerMock);

            producerMock.Setup(p => p.Flush(It.IsAny<TimeSpan>())).Verifiable();
            producerMock.Setup(p => p.Dispose()).Verifiable();

            sut.Dispose();

            producerMock.Verify(p => p.Flush(It.IsAny<TimeSpan>()), Times.Once);
            producerMock.Verify(p => p.Dispose(), Times.Once);
        }

        // small helper stream that is not readable
        private class NonReadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => throw new NotSupportedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
