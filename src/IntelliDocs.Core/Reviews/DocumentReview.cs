namespace IntelliDocs.Core.Reviews;

public sealed class DocumentReview
{
    private readonly List<DocumentReviewCorrection> _corrections = [];

    private DocumentReview()
    {
    }

    private DocumentReview(
        Guid documentId,
        string reviewer,
        DateTime startedAtUtc)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        Reviewer = reviewer;
        StartedAtUtc = startedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public string Reviewer { get; private set; } = string.Empty;

    public DateTime StartedAtUtc { get; private set; }

    public DocumentReviewDecision? Decision { get; private set; }

    public string? DecisionReason { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public IReadOnlyCollection<DocumentReviewCorrection> Corrections =>
        _corrections.AsReadOnly();

    public bool IsCompleted =>
        Decision.HasValue;

    public static DocumentReview Start(
        Guid documentId,
        string reviewer,
        DateTime? startedAtUtc = null)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Document ID is required.",
                nameof(documentId));
        }

        if (string.IsNullOrWhiteSpace(reviewer))
        {
            throw new ArgumentException(
                "Reviewer is required.",
                nameof(reviewer));
        }

        return new DocumentReview(
            documentId,
            reviewer.Trim(),
            startedAtUtc ?? DateTime.UtcNow);
    }

    public void AddCorrection(
        string fieldName,
        string? originalValue,
        string? correctedValue,
        string reviewer,
        DateTime? correctedAtUtc = null)
    {
        EnsureOpen();

        if (!string.Equals(
                Reviewer,
                reviewer?.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Only the assigned reviewer can add corrections.");
        }

        _corrections.Add(
            new DocumentReviewCorrection(
                Id,
                fieldName,
                originalValue,
                correctedValue,
                Reviewer,
                correctedAtUtc ?? DateTime.UtcNow));
    }

    public void Complete(
        DocumentReviewDecision decision,
        string reviewer,
        string? reason = null,
        DateTime? decidedAtUtc = null)
    {
        EnsureOpen();

        if (!string.Equals(
                Reviewer,
                reviewer?.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Only the assigned reviewer can complete the review.");
        }

        Decision = decision;
        DecisionReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim();
        DecidedAtUtc = decidedAtUtc ?? DateTime.UtcNow;
    }

    private void EnsureOpen()
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException(
                "The review has already been completed.");
        }
    }
}
