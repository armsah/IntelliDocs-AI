namespace IntelliDocs.Core.Documents;

public sealed class DocumentJob
{
    private static readonly IReadOnlyDictionary<DocumentStatus, HashSet<DocumentStatus>>
        AllowedTransitions =
            new Dictionary<DocumentStatus, HashSet<DocumentStatus>>
            {
                [DocumentStatus.Submitted] =
                [
                    DocumentStatus.Stored
                ],

                [DocumentStatus.Stored] =
                [
                    DocumentStatus.Queued
                ],

                [DocumentStatus.Queued] =
                [
                    DocumentStatus.Processing
                ],

                [DocumentStatus.Processing] =
                [
                    DocumentStatus.Extracted,
                    DocumentStatus.Failed,
                    DocumentStatus.DeadLettered
                ],

                [DocumentStatus.Extracted] =
                [
                    DocumentStatus.Validating
                ],

                [DocumentStatus.Validating] =
                [
                    DocumentStatus.Approved,
                    DocumentStatus.NeedsReview
                ],

                [DocumentStatus.NeedsReview] =
                [
                    DocumentStatus.InReview
                ],

                [DocumentStatus.InReview] =
                [
                    DocumentStatus.Approved,
                    DocumentStatus.Rejected
                ],

                [DocumentStatus.Approved] =
                [
                    DocumentStatus.Publishing
                ],

                [DocumentStatus.Publishing] =
                [
                    DocumentStatus.Completed,
                    DocumentStatus.Failed
                ],

                [DocumentStatus.Failed] =
                [
                    DocumentStatus.Queued
                ],

                [DocumentStatus.DeadLettered] =
                [
                    DocumentStatus.Queued
                ]
            };

    private readonly List<DocumentJobTransition> _transitions = [];

    private DocumentJob()
    {
    }

    private DocumentJob(
        Guid documentId,
        string tenantId,
        string originalFileName,
        string sha256,
        DateTime submittedAtUtc)
    {
        DocumentId = documentId;
        TenantId = tenantId;
        OriginalFileName = originalFileName;
        Sha256 = sha256;
        ProcessingStatus = DocumentStatus.Submitted;
        SubmittedAtUtc = submittedAtUtc;
    }

    public Guid DocumentId { get; private set; }

    public string TenantId { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string? OriginalStorageUri { get; private set; }

    public string Sha256 { get; private set; } = string.Empty;

    public string? DetectedType { get; private set; }

    public string? AiModelId { get; private set; }

    public string? AiModelVersion { get; private set; }

    public DocumentStatus ProcessingStatus { get; private set; }

    public DateTime SubmittedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public IReadOnlyCollection<DocumentJobTransition> Transitions =>
        _transitions.AsReadOnly();

    public static DocumentJob Create(
        string tenantId,
        string originalFileName,
        string sha256,
        DateTime? submittedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException(
                "Original file name is required.",
                nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException(
                "SHA-256 hash is required.",
                nameof(sha256));
        }

        return new DocumentJob(
            Guid.NewGuid(),
            tenantId.Trim(),
            originalFileName.Trim(),
            sha256.Trim().ToLowerInvariant(),
            submittedAtUtc ?? DateTime.UtcNow);
    }

    public void SetStorageUri(string storageUri)
    {
        if (string.IsNullOrWhiteSpace(storageUri))
        {
            throw new ArgumentException(
                "Storage URI is required.",
                nameof(storageUri));
        }

        OriginalStorageUri = storageUri.Trim();
    }

    public void SetDocumentType(string detectedType)
    {
        if (string.IsNullOrWhiteSpace(detectedType))
        {
            throw new ArgumentException(
                "Detected document type is required.",
                nameof(detectedType));
        }

        DetectedType = detectedType.Trim();
    }

    public void SetAiModel(string modelId, string? modelVersion)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException(
                "AI model ID is required.",
                nameof(modelId));
        }

        AiModelId = modelId.Trim();
        AiModelVersion = string.IsNullOrWhiteSpace(modelVersion)
            ? null
            : modelVersion.Trim();
    }

    public void TransitionTo(
        DocumentStatus nextStatus,
        string actor,
        string processingStage,
        string? reason = null,
        DateTime? occurredAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException(
                "Transition actor is required.",
                nameof(actor));
        }

        if (string.IsNullOrWhiteSpace(processingStage))
        {
            throw new ArgumentException(
                "Processing stage is required.",
                nameof(processingStage));
        }

        if (!CanTransitionTo(nextStatus))
        {
            throw new InvalidDocumentStateTransitionException(
                ProcessingStatus,
                nextStatus);
        }

        var previousStatus = ProcessingStatus;
        var timestamp = occurredAtUtc ?? DateTime.UtcNow;

        ProcessingStatus = nextStatus;

        if (nextStatus == DocumentStatus.Completed)
        {
            CompletedAtUtc = timestamp;
        }

        _transitions.Add(
            new DocumentJobTransition(
                DocumentId,
                previousStatus,
                nextStatus,
                timestamp,
                actor.Trim(),
                processingStage.Trim(),
                string.IsNullOrWhiteSpace(reason)
                    ? null
                    : reason.Trim()));
    }

    public bool CanTransitionTo(DocumentStatus nextStatus)
    {
        return AllowedTransitions.TryGetValue(
                   ProcessingStatus,
                   out var allowedStatuses)
               && allowedStatuses.Contains(nextStatus);
    }
}
