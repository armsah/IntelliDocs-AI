using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using IntelliDocs.Core.DocumentClassification;
using IntelliDocs.Core.DocumentIntelligence;
using IntelliDocs.Core.Storage;
using IntelliDocs.Infrastructure.DocumentIntelligence;
using IntelliDocs.Infrastructure.Messaging;
using IntelliDocs.Infrastructure.Persistence;
using IntelliDocs.Infrastructure.Storage;
using IntelliDocs.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

var databaseConnectionString =
    builder.Configuration.GetConnectionString("PostgreSql")
    ?? throw new InvalidOperationException(
        "Connection string 'PostgreSql' is not configured.");

builder.Services.AddDbContextFactory<IntelliDocsDbContext>(
    options =>
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

builder.Services.AddSingleton<
    IDocumentClassifier,
    AzureDocumentClassifier>();

builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(
        ServiceBusOptions.SectionName));

builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var options = sp
        .GetRequiredService<IOptions<ServiceBusOptions>>()
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

builder.Services.AddSingleton(sp =>
{
    var client =
        sp.GetRequiredService<ServiceBusClient>();

    var options = sp
        .GetRequiredService<IOptions<ServiceBusOptions>>()
        .Value;

    if (string.IsNullOrWhiteSpace(
            options.DocumentProcessingQueueName))
    {
        throw new InvalidOperationException(
            "ServiceBus:DocumentProcessingQueueName is required.");
    }

    return client.CreateProcessor(
        options.DocumentProcessingQueueName,
        new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
            PrefetchCount = 0,
            MaxAutoLockRenewalDuration =
                TimeSpan.FromMinutes(5)
        });
});

builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection(
        WorkerOptions.SectionName));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();