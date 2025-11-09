using TransactionDispatch.Infrastructure;
using TransactionDispatch.Application.Options;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Infrastructure.FileSystem;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);

// bind allowed file types
builder.Services.Configure<AllowedFileTypesOptions>(builder.Configuration);

builder.Services.AddSingleton<IFileProvider, FileSystemFileProvider>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
