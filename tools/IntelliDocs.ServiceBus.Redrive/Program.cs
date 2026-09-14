using System.Text.Json;
using Azure.Messaging.ServiceBus;
using IntelliDocs.Core.Documents;
using IntelliDocs.Core.Messaging;
using IntelliDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

const string defaultQueueName = "document-processing";
const string actor = "service-bus-redrive";

if (args.Length != 1 ||
    !Guid.TryParse(args[0], out var documentId))
{
    Console.Error.WriteLine(
        "Usage: IntelliDocs.ServiceBus.Redrive <document-id>");

    return 2;
}

var serviceBusConnectionString =
    Environment.GetEnvironmentVariable(
        "INTELLIDOCS_SERVICEBUS_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(
        serviceBusConnectionString))
{
    Console.Error.WriteLine(
        "Environment variable " +
        "INTELLIDOCS_SERVICEBUS_CONNECTION_STRING is required.");

    return 2;
}

var postgresqlConnectionString =
    Environment.GetEnvironmentVariable(
        "INTELLIDOCS_POSTGRESQL_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(
        postgresqlConnectionString))
{
    Console.Error.WriteLine(
        "Environment variable " +
        "INTELLIDOCS_POSTGRESQL_CONNECTION_STRING is required.");

    return 2;
}

var queueName =
    Environment.GetEnvironmentVariable(
        "INTELLIDOCS_SERVICEBUS_QUEUE_NAME");

if (string.IsNullOrWhiteSpace(queueName))
{
    queueName = defaultQueueName;
}

var dbOptions =
    new DbContextOptionsBuilder<IntelliDocsDbContext>()
        .UseNpgsql(postgresqlConnectionString)
        .Options;

await using var dbContext =
    new IntelliDocsDbContext(dbOptions);

var job =
    await dbContext.DocumentJobs
        .Include(x => x.Transitions)
        .SingleOrDefaultAsync(
            x => x.DocumentId == documentId);

if (job is null)
{
    Console.Error.WriteLine(
        $"Document {documentId} was not found.");

    return 3;
}

if (job.ProcessingStatus is not
    (DocumentStatus.DeadLettered or
     DocumentStatus.Queued))
{
    Console.Error.WriteLine(
        $"Document {documentId} is in state " +
        $"{job.ProcessingStatus}. " +
        "Only DeadLettered or an already re-drive Queued " +
        "document can be processed.");

    return 4;
}

await using var serviceBusClient =
    new ServiceBusClient(
        serviceBusConnectionString);

await using var deadLetterReceiver =
    serviceBusClient.CreateReceiver(
        queueName,
        new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter,
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });

await using var activeSender =
    serviceBusClient.CreateSender(
        queueName);

Console.WriteLine(
    $"Searching DLQ '{queueName}' for document {documentId}...");

var deadLetterMessage =
    await FindDeadLetterMessageAsync(
        deadLetterReceiver,
        documentId);

if (deadLetterMessage is null)
{
    Console.Error.WriteLine(
        $"No DLQ message was found for document {documentId}.");

    return 5;
}

DocumentProcessingMessage originalMessage;

try
{
    var jsonOptions =
        new JsonSerializerOptions(
            JsonSerializerDefaults.Web);

    originalMessage =
        JsonSerializer.Deserialize<DocumentProcessingMessage>(
            deadLetterMessage.Body.ToString(),
            jsonOptions)
        ?? throw new JsonException(
            "Message body deserialized to null.");
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        "The matching DLQ message could not be deserialized: " +
        exception.Message);

    return 6;
}

if (originalMessage.DocumentId != documentId)
{
    Console.Error.WriteLine(
        "DLQ message document ID does not match " +
        "the requested document.");

    return 6;
}

if (!string.Equals(
        originalMessage.TenantId,
        job.TenantId,
        StringComparison.Ordinal))
{
    Console.Error.WriteLine(
        "DLQ message tenant does not match " +
        "the persisted document tenant.");

    return 6;
}

var redriveMessage =
    originalMessage with
    {
        EnqueuedAtUtc = DateTimeOffset.UtcNow,
        RedriveCount =
            originalMessage.RedriveCount + 1
    };

if (job.ProcessingStatus ==
    DocumentStatus.DeadLettered)
{
    job.TransitionTo(
        DocumentStatus.Queued,
        actor,
        redriveMessage.ProcessingStage,
        $"Re-drive generation r{redriveMessage.RedriveCount} " +
        "requested from Service Bus DLQ.");

    await dbContext.SaveChangesAsync();

    Console.WriteLine(
        "PostgreSQL state transitioned: " +
        "DeadLettered -> Queued.");
}
else
{
    Console.WriteLine(
        "Document is already Queued. " +
        "Continuing the same re-drive generation.");
}

var outgoingMessage =
    CreateServiceBusMessage(
        redriveMessage);

try
{
    await activeSender.SendMessageAsync(
        outgoingMessage);

    Console.WriteLine(
        $"Published replacement message: " +
        $"{redriveMessage.MessageId}");
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        "Replacement publication failed. " +
        "The original DLQ message has NOT been completed.");

    Console.Error.WriteLine(
        exception.Message);

    return 7;
}

try
{
    await deadLetterReceiver.CompleteMessageAsync(
        deadLetterMessage);

    Console.WriteLine(
        "Original DLQ message completed.");
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        "Replacement was published, but completing the " +
        "original DLQ message failed.");

    Console.Error.WriteLine(
        "Re-running this command is safe for the same " +
        "DLQ message because the replacement uses the " +
        "same generation-specific MessageId.");

    Console.Error.WriteLine(
        exception.Message);

    return 8;
}

Console.WriteLine();
Console.WriteLine("Re-drive completed successfully.");
Console.WriteLine(
    $"DocumentId: {documentId}");
Console.WriteLine(
    $"ProcessingStage: {redriveMessage.ProcessingStage}");
Console.WriteLine(
    $"RedriveCount: {redriveMessage.RedriveCount}");
Console.WriteLine(
    $"IdempotencyKey: {redriveMessage.IdempotencyKey}");
Console.WriteLine(
    $"MessageId: {redriveMessage.MessageId}");

return 0;

static async Task<ServiceBusReceivedMessage?>
    FindDeadLetterMessageAsync(
        ServiceBusReceiver receiver,
        Guid documentId)
{
    const int maxMessagesToInspect = 100;

    var inspected = 0;

    while (inspected < maxMessagesToInspect)
    {
        var messages =
            await receiver.ReceiveMessagesAsync(
                maxMessages: 20,
                maxWaitTime: TimeSpan.FromSeconds(3));

        if (messages.Count == 0)
        {
            return null;
        }

        foreach (var message in messages)
        {
            inspected++;

            if (IsForDocument(
                    message,
                    documentId))
            {
                foreach (var otherMessage in messages)
                {
                    if (otherMessage != message)
                    {
                        await receiver.AbandonMessageAsync(
                            otherMessage);
                    }
                }

                return message;
            }

            await receiver.AbandonMessageAsync(
                message);

            if (inspected >=
                maxMessagesToInspect)
            {
                break;
            }
        }
    }

    return null;
}

static bool IsForDocument(
    ServiceBusReceivedMessage message,
    Guid documentId)
{
    if (message.ApplicationProperties.TryGetValue(
            "documentId",
            out var applicationDocumentId) &&
        Guid.TryParse(
            applicationDocumentId?.ToString(),
            out var parsedDocumentId))
    {
        return parsedDocumentId == documentId;
    }

    try
    {
        var jsonOptions =
            new JsonSerializerOptions(
                JsonSerializerDefaults.Web);

        var body =
            JsonSerializer.Deserialize<DocumentProcessingMessage>(
                message.Body.ToString(),
                jsonOptions);

        return body?.DocumentId ==
               documentId;
    }
    catch
    {
        return false;
    }
}

static ServiceBusMessage CreateServiceBusMessage(
    DocumentProcessingMessage message)
{
    var jsonOptions =
        new JsonSerializerOptions(
            JsonSerializerDefaults.Web);

    var serviceBusMessage =
        new ServiceBusMessage(
            BinaryData.FromObjectAsJson(
                message,
                jsonOptions))
        {
            MessageId =
                message.MessageId,
            ContentType =
                "application/json",
            Subject =
                message.ProcessingStage,
            CorrelationId =
                message.DocumentId.ToString("N")
        };

    serviceBusMessage
        .ApplicationProperties["documentId"] =
        message.DocumentId.ToString();

    serviceBusMessage
        .ApplicationProperties["tenantId"] =
        message.TenantId;

    serviceBusMessage
        .ApplicationProperties["processingStage"] =
        message.ProcessingStage;

    serviceBusMessage
        .ApplicationProperties["redriveCount"] =
        message.RedriveCount;

    return serviceBusMessage;
}