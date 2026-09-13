using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using IntelliDocs.Core.Storage;
using IntelliDocs.Infrastructure.Persistence;
using IntelliDocs.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using IntelliDocs.Core.DocumentIntelligence;
using IntelliDocs.Infrastructure.DocumentIntelligence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var databaseConnectionString =
    builder.Configuration.GetConnectionString("PostgreSql")
    ?? throw new InvalidOperationException(
        "Connection string 'PostgreSql' is not configured.");

builder.Services.AddDbContext<IntelliDocsDbContext>(options =>
    options.UseNpgsql(databaseConnectionString));

var blobStorageConnectionString =
    builder.Configuration.GetConnectionString("BlobStorage")
    ?? throw new InvalidOperationException(
        "Connection string 'BlobStorage' is not configured.");

builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(
        BlobStorageOptions.SectionName));

builder.Services.AddSingleton(
    new BlobServiceClient(blobStorageConnectionString));

builder.Services.AddSingleton<
    IDocumentStorage,
    AzureBlobDocumentStorage>();

builder.Services.Configure<DocumentIntelligenceOptions>(
    builder.Configuration.GetSection(
        DocumentIntelligenceOptions.SectionName));

builder.Services.AddSingleton<
    IDocumentIntelligenceProvider,
    AzureDocumentIntelligenceProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "healthy",
        service = "IntelliDocs.Api"
    }));

app.Run();

public partial class Program;
