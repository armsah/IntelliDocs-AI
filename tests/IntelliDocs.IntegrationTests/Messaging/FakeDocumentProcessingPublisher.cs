using System.Collections.Concurrent;
using IntelliDocs.Core.Messaging;

namespace IntelliDocs.IntegrationTests.Messaging;

public sealed class FakeDocumentProcessingPublisher
    : IDocumentProcessingPublisher
{
    private readonly ConcurrentQueue<DocumentProcessingMessage>
        _messages = new();

    public IReadOnlyList<DocumentProcessingMessage> Messages =>
        _messages.ToArray();

    public Task PublishAsync(
        DocumentProcessingMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Enqueue(message);

        return Task.CompletedTask;
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }
}