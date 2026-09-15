namespace IntelliDocs.Api.Models.Reviews;

public sealed record DocumentReviewCorrectionResponse(
    Guid Id,
    string FieldName,
    string? OriginalValue,
    string? CorrectedValue,
    string Reviewer,
    DateTime CorrectedAtUtc);
