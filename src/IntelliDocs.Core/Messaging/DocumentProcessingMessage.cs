namespace IntelliDocs.Core.Messaging;

public sealed record DocumentProcessingMessage(
    Guid DocumentId,
    string TenantId,
    string ProcessingStage,
    DateTimeOffset EnqueuedAtUtc,
    int RedriveCount = 0)
{
    public string IdempotencyKey =>
        $"{DocumentId:N}:{ProcessingStage.ToLowerInvariant()}";

    public string MessageId =>
        $"{IdempotencyKey}:r{RedriveCount}";
}