namespace TransactionDispatch.Application.Options
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string Topic { get; set; } = "transactions.raw";
        public string? Acks { get; set; }
        public bool EnableIdempotence { get; set; } = true;
        public int MessageTimeoutMs { get; set; } = 30000;
        public string? CompressionType { get; set; }
        public int? BatchSize { get; set; }
        public int? LingerMs { get; set; }
    }
}
