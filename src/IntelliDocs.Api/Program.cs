using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;
using IntelliDocs.Core.Storage;
using IntelliDocs.Infrastructure.Persistence;
using IntelliDocs.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using IntelliDocs.Core.DocumentIntelligence;
using IntelliDocs.Infrastructure.DocumentIntelligence;
using IntelliDocs.Core.Messaging;
using IntelliDocs.Infrastructure.Messaging;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using IntelliDocs.Infrastructure.Observability;


var builder = WebApplication.CreateBuilder(args);

var applicationInsightsConnectionString =
    builder.Configuration[
        "APPLICATIONINSIGHTS_CONNECTION_STRING"];

if (!string.IsNullOrWhiteSpace(
        applicationInsightsConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString =
                applicationInsightsConnectionString;
        })
        .WithMetrics(metrics =>
            metrics.AddMeter(
                IntelliDocsTelemetry.MeterName))
        .WithTracing(tracing =>
            tracing.AddSource(
                IntelliDocsTelemetry.SourceName));
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var databaseConnectionString =
    builder.Configuration.GetConnectionString("PostgreSql")
    ?? throw new InvalidOperationException(
        "Connection string 'PostgreSql' is not configured.");

builder.Services.AddDbContext<IntelliDocsDbContext>(options =>
    options.UseNpgsql(databaseConnectionString));

builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(
        BlobStorageOptions.SectionName));

builder.Services.AddSingleton<BlobServiceClient>(sp =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("BlobStorage");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return new BlobServiceClient(connectionString);
    }

    var serviceUri =
        builder.Configuration[
            $"{BlobStorageOptions.SectionName}:ServiceUri"];

    if (string.IsNullOrWhiteSpace(serviceUri))
    {
        throw new InvalidOperationException(
            "Blob Storage connection string or service URI is required.");
    }

    var credential =
        sp.GetRequiredService<TokenCredential>();

    return new BlobServiceClient(
        new Uri(serviceUri),
        credential);
});

builder.Services.AddSingleton<
    IDocumentStorage,
    AzureBlobDocumentStorage>();

builder.Services.Configure<DocumentIntelligenceOptions>(
    builder.Configuration.GetSection(
        DocumentIntelligenceOptions.SectionName));

builder.Services.AddSingleton<TokenCredential>(
    _ => new DefaultAzureCredential());

builder.Services.AddSingleton<
    IDocumentIntelligenceProvider,
    AzureDocumentIntelligenceProvider>();

builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(
        ServiceBusOptions.SectionName));

builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var options = sp
        .GetRequiredService<
            Microsoft.Extensions.Options.IOptions<ServiceBusOptions>>()
        .Value;

    if (!string.IsNullOrWhiteSpace(options.ConnectionString))
    {
        return new ServiceBusClient(
            options.ConnectionString);
    }

    if (string.IsNullOrWhiteSpace(
            options.FullyQualifiedNamespace))
    {
        throw new InvalidOperationException(
            "Service Bus connection string or fully qualified namespace is required.");
    }

    var credential =
        sp.GetRequiredService<TokenCredential>();

    return new ServiceBusClient(
        options.FullyQualifiedNamespace,
        credential);
});

builder.Services.AddSingleton<
    IDocumentProcessingPublisher,
    AzureServiceBusDocumentProcessingPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Map(
    "/health",
    healthApp =>
    {
        healthApp.Run(
            async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status200OK;

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        status = "healthy",
                        service = "IntelliDocs.Api"
                    });
            });
    });

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
