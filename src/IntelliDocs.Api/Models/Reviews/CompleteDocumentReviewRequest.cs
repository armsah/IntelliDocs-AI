using IntelliDocs.Core.Reviews;

namespace IntelliDocs.Api.Models.Reviews;

public sealed record CompleteDocumentReviewRequest(
    DocumentReviewDecision Decision,
    string? Reason);