using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Application.Options;

namespace TransactionDispatch.Infrastructure.Kafka
{
    public class ConfluentKafkaProducer : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, byte[]> _producer;
        private readonly KafkaOptions _options;
        private readonly ILogger<ConfluentKafkaProducer> _logger;
        private bool _disposed;

        public ConfluentKafkaProducer(ProducerConfig config, KafkaOptions options, ILogger<ConfluentKafkaProducer> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _producer = new ProducerBuilder<string, byte[]>(config)
                .SetErrorHandler((p, e) => _logger.LogError("Kafka producer error: {Reason}", e.Reason))
                .Build();
        }
        public async Task ProduceAsync(Stream payloadStream, string key, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ConfluentKafkaProducer));
            if (payloadStream == null) throw new ArgumentNullException(nameof(payloadStream));
            if (!payloadStream.CanRead) throw new ArgumentException("payloadStream must be readable", nameof(payloadStream));

            // Read stream into memory. For 20KB average messages this is fine, adjust for much larger payloads.
            byte[] bytes;
            if (payloadStream is MemoryStream ms)
            {
                bytes = ms.ToArray();
            }
            else
            {
                using var memorystream = new MemoryStream();
                await payloadStream.CopyToAsync(memorystream, cancellationToken).ConfigureAwait(false);
                bytes = memorystream.ToArray();
            }

            var message = new Message<string, byte[]>
            {
                Key = key,
                Value = bytes
            };

            // Topic from config
            var topic = _options.Topic ?? throw new InvalidOperationException("Kafka topic not configured.");

            try
            {
                // ProduceAsync returns delivery report
                var dr = await _producer.ProduceAsync(topic, message, cancellationToken).ConfigureAwait(false);

                if (dr.Status != PersistenceStatus.Persisted)
                {
                    throw new Exception($"Kafka delivery failed. Status: {dr.Status}");
                }
            }
            catch (ProduceException<string, byte[]> pex)
            {
                _logger.LogError(pex, "Kafka produce exception for key {Key}", key);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(5));
                _producer.Dispose();
            }
            catch { /* swallow */ }
            _disposed = true;
        }
    }
}
