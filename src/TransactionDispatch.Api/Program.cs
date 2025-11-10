using Microsoft.Extensions.Options;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Application.Options;
using TransactionDispatch.Application.Ports;
using TransactionDispatch.Application.Services;
using TransactionDispatch.Infrastructure;
using TransactionDispatch.Infrastructure.Background;
using TransactionDispatch.Infrastructure.IO;
using TransactionDispatch.Infrastructure.Kafka;
using TransactionDispatch.Infrastructure.Persistence;
using TransactionDispatch.Infrastructure.Services;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Configuration & Options
// ----------------------
builder.Services.Configure<AllowedFileTypesOptions>(builder.Configuration);
builder.Services.Configure<FileProviderOptions>(builder.Configuration.GetSection("FileProvider"));
builder.Services.Configure<DispatchOptions>(builder.Configuration.GetSection("Dispatch"));
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));

// ----------------------
// Infrastructure (DbContext, repositories, kafka, etc.)
// ----------------------
builder.Services.AddInfrastructure(builder.Configuration);

// ----------------------
// Application / Infrastructure services
// ----------------------
AddApplicationServices(builder.Services);

// ----------------------
// MVC / Swagger / Logging
// ----------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ----------------------
// Build & run
// ----------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

void AddApplicationServices(IServiceCollection services)
{
    // Application layer
    services.AddScoped<IDispatchService, DispatchService>();

    // File provider - scoped
    services.AddScoped<IFileProvider, LocalFileProvider>();

    // Background queue - singleton
    services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

    // Job/File processors - scoped
    services.AddScoped<IJobProcessor, JobProcessor>();
    services.AddScoped<IFileProcessor, FileProcessor>();

    // Kafka producer - singleton with Confluent config
    services.AddSingleton<IKafkaProducer>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
        var logger = sp.GetRequiredService<ILogger<ConfluentKafkaProducer>>();

        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            Acks = Enum.TryParse<Acks>(options.Acks ?? "All", true, out var parsedAcks) ? parsedAcks : Acks.All,
            EnableIdempotence = options.EnableIdempotence,
            MessageTimeoutMs = options.MessageTimeoutMs,
            CompressionType = Enum.TryParse<Confluent.Kafka.CompressionType>(options.CompressionType, true, out var cType)
                ? cType
                : Confluent.Kafka.CompressionType.Lz4,
            BatchSize = options.BatchSize ?? 32768,
            LingerMs = options.LingerMs ?? 5
        };

        return new ConfluentKafkaProducer(config, options, logger);
    });

    // Background worker (hosted service)
    services.AddHostedService<DispatchBackgroundService>();
}
