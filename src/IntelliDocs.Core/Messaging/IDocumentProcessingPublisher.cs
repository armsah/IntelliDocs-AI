namespace IntelliDocs.Core.Messaging;

public interface IDocumentProcessingPublisher
{
    Task PublishAsync(
        DocumentProcessingMessage message,
        CancellationToken cancellationToken = default);
}