namespace IntelliDocs.Core.Documents;

public sealed class DocumentJobTransition
{
    private DocumentJobTransition()
    {
    }

    internal DocumentJobTransition(
        Guid documentId,
        DocumentStatus previousStatus,
        DocumentStatus nextStatus,
        DateTime occurredAtUtc,
        string actor,
        string processingStage,
        string? reason)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        PreviousStatus = previousStatus;
        NextStatus = nextStatus;
        OccurredAtUtc = occurredAtUtc;
        Actor = actor;
        ProcessingStage = processingStage;
        Reason = reason;
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public DocumentStatus PreviousStatus { get; private set; }

    public DocumentStatus NextStatus { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string Actor { get; private set; } = string.Empty;

    public string ProcessingStage { get; private set; } = string.Empty;

    public string? Reason { get; private set; }
}
