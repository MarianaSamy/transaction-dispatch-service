using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
            if (config == null) throw new ArgumentNullException(nameof(config));

            // Apply options into the provided config, but only when the config value is "unset" or default.
            ApplyOptionsToConfigSafely(config, _options);

            _producer = new ProducerBuilder<string, byte[]>(config)
                .SetErrorHandler((p, e) => _logger.LogError("Kafka producer error: {Reason}", e.Reason))
                .Build();
        }

        private static void ApplyOptionsToConfigSafely(ProducerConfig config, KafkaOptions options)
        {
            // BootstrapServers
            if (string.IsNullOrWhiteSpace(config.BootstrapServers) && !string.IsNullOrWhiteSpace(options.BootstrapServers))
                config.BootstrapServers = options.BootstrapServers;

            // EnableIdempotence
            // Only set if config hasn't been set explicitly (default false)
            if (!config.TryGetBool("enable.idempotence", out var _ei) && options.EnableIdempotence)
                config.EnableIdempotence = options.EnableIdempotence;

            // MessageTimeoutMs
            if ((config.MessageTimeoutMs == 0 || config.MessageTimeoutMs == default) && options.MessageTimeoutMs > 0)
                config.MessageTimeoutMs = options.MessageTimeoutMs;

            // BatchSize
            if ((config.BatchSize == 0 || config.BatchSize == default) && options.BatchSize.HasValue)
                config.BatchSize = options.BatchSize.Value;

            // LingerMs
            if ((config.LingerMs == 0 || config.LingerMs == default) && options.LingerMs.HasValue)
                config.LingerMs = options.LingerMs.Value;

            // CompressionType (parse string option)
            if (!string.IsNullOrWhiteSpace(options.CompressionType) && (config.CompressionType == CompressionType.None))
            {
                if (Enum.TryParse<CompressionType>(options.CompressionType, true, out var comp))
                    config.CompressionType = comp;
            }

            // Acks (string -> enum)
            if (!string.IsNullOrWhiteSpace(options.Acks) && (config.Acks == default))
            {
                if (Enum.TryParse<Acks>(options.Acks, true, out var acks))
                    config.Acks = acks;
            }

            // Note: we intentionally do not set topic on ProducerConfig (topic is application-level option)
        }

        public async Task ProduceAsync(Stream payloadStream, string key, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ConfluentKafkaProducer));
            if (payloadStream == null) throw new ArgumentNullException(nameof(payloadStream));
            if (!payloadStream.CanRead) throw new ArgumentException("payloadStream must be readable", nameof(payloadStream));

            byte[] bytes;
            if (payloadStream is MemoryStream ms)
            {
                bytes = ms.ToArray();
            }
            else
            {
                await using var memorystream = new MemoryStream();
                await payloadStream.CopyToAsync(memorystream, cancellationToken).ConfigureAwait(false);
                bytes = memorystream.ToArray();
            }

            var message = new Message<string, byte[]>
            {
                Key = key,
                Value = bytes
            };

            var topic = _options.Topic ?? throw new InvalidOperationException("Kafka topic not configured.");

            try
            {
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
            catch
            {
                // swallow non-fatal disposal errors
            }
            _disposed = true;
        }
    }

    // small extension methods to check raw config presence for compatibility across Confluent versions
    internal static class ProducerConfigCompatibility
    {
        public static bool TryGetBool(this ProducerConfig config, string key, out bool value)
        {
            value = false;
            try
            {
                // ProducerConfig exposes indexer in recent clients; try to read it
                if (config == null) return false;
                var dict = (config as System.Collections.IDictionary) ?? null;
                if (dict != null && dict.Contains(key))
                {
                    var obj = dict[key];
                    if (obj is bool b) { value = b; return true; }
                    if (bool.TryParse(obj?.ToString() ?? string.Empty, out var parsed)) { value = parsed; return true; }
                }
            }
            catch
            {
                // ignore compatibility lookup errors
            }
            return false;
        }
    }
}
