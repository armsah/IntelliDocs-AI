using IntelliDocs.Core.Documents;
using IntelliDocs.Core.Reviews;

namespace IntelliDocs.Api.Models.Reviews;

public sealed record DocumentReviewResponse(
    Guid ReviewId,
    Guid DocumentId,
    DocumentStatus ProcessingStatus,
    string TenantId,
    string OriginalFileName,
    string? DetectedType,
    string Reviewer,
    DateTime StartedAtUtc,
    DocumentReviewDecision? Decision,
    string? DecisionReason,
    DateTime? DecidedAtUtc,
    IReadOnlyList<DocumentReviewCorrectionResponse> Corrections,
    string? MachineResultJson);
