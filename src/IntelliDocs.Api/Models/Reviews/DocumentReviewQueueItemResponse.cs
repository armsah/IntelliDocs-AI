using IntelliDocs.Core.Documents;

namespace IntelliDocs.Api.Models.Reviews;

public sealed record DocumentReviewQueueItemResponse(
    Guid DocumentId,
    string TenantId,
    string OriginalFileName,
    string? DetectedType,
    DocumentStatus ProcessingStatus,
    DateTime SubmittedAtUtc);
