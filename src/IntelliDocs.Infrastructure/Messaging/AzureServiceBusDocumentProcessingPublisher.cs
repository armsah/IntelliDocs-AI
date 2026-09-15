using System.Text.Json;
using Azure.Messaging.ServiceBus;
using IntelliDocs.Core.Messaging;
using Microsoft.Extensions.Options;

namespace IntelliDocs.Infrastructure.Messaging;

public sealed class AzureServiceBusDocumentProcessingPublisher
    : IDocumentProcessingPublisher,
      IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly JsonSerializerOptions _jsonOptions =
        new(JsonSerializerDefaults.Web);

    public AzureServiceBusDocumentProcessingPublisher(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        var value = options.Value;

        if (string.IsNullOrWhiteSpace(
                value.DocumentProcessingQueueName))
        {
            throw new InvalidOperationException(
                "ServiceBus:DocumentProcessingQueueName is required.");
        }

        _sender = client.CreateSender(
            value.DocumentProcessingQueueName);
    }

    public async Task PublishAsync(
        DocumentProcessingMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var body = BinaryData.FromObjectAsJson(
            message,
            _jsonOptions);

        var serviceBusMessage = new ServiceBusMessage(body)
        {
            MessageId = message.MessageId,
            ContentType = "application/json",
            Subject = message.ProcessingStage,
            CorrelationId = message.DocumentId.ToString("N")
        };

        serviceBusMessage.ApplicationProperties["documentId"] =
            message.DocumentId.ToString();

        serviceBusMessage.ApplicationProperties["tenantId"] =
            message.TenantId;

        serviceBusMessage.ApplicationProperties["processingStage"] =
            message.ProcessingStage;

        serviceBusMessage.ApplicationProperties["redriveCount"] =
            message.RedriveCount;

        await _sender.SendMessageAsync(
            serviceBusMessage,
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
    }
}