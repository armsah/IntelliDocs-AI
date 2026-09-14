namespace IntelliDocs.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string FullyQualifiedNamespace { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public string DocumentProcessingQueueName { get; set; } =
        "document-processing";
}