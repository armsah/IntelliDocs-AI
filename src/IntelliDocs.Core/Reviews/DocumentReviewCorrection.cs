namespace IntelliDocs.Core.Reviews;

public sealed class DocumentReviewCorrection
{
    private DocumentReviewCorrection()
    {
    }

    internal DocumentReviewCorrection(
        Guid reviewId,
        string fieldName,
        string? originalValue,
        string? correctedValue,
        string reviewer,
        DateTime correctedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException(
                "Field name is required.",
                nameof(fieldName));
        }

        if (string.IsNullOrWhiteSpace(reviewer))
        {
            throw new ArgumentException(
                "Reviewer is required.",
                nameof(reviewer));
        }

        Id = Guid.NewGuid();
        ReviewId = reviewId;
        FieldName = fieldName.Trim();
        OriginalValue = originalValue;
        CorrectedValue = correctedValue;
        Reviewer = reviewer.Trim();
        CorrectedAtUtc = correctedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ReviewId { get; private set; }

    public string FieldName { get; private set; } = string.Empty;

    public string? OriginalValue { get; private set; }

    public string? CorrectedValue { get; private set; }

    public string Reviewer { get; private set; } = string.Empty;

    public DateTime CorrectedAtUtc { get; private set; }
}
