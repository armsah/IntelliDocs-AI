namespace IntelliDocs.Api.Models.Reviews;

public sealed record AddDocumentReviewCorrectionRequest(
    string FieldName,
    string? OriginalValue,
    string? CorrectedValue,
    string Reviewer);
