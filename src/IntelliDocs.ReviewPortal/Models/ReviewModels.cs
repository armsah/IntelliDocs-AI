using System.Text.Json.Serialization;

namespace IntelliDocs.ReviewPortal.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentStatus
{
    Submitted = 0,
    Stored = 1,
    Queued = 2,
    Processing = 3,
    Extracted = 4,
    Validating = 5,
    Approved = 6,
    NeedsReview = 7,
    InReview = 8,
    Rejected = 9,
    Publishing = 10,
    Completed = 11,
    Failed = 12,
    DeadLettered = 13
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentReviewDecision
{
    Approved = 0,
    Rejected = 1
}

public sealed record DocumentReviewQueueItem(
    Guid DocumentId,
    string TenantId,
    string OriginalFileName,
    string? DetectedType,
    DocumentStatus ProcessingStatus,
    DateTime SubmittedAtUtc);

public sealed record DocumentReviewCorrection(
    Guid Id,
    string FieldName,
    string? OriginalValue,
    string? CorrectedValue,
    string Reviewer,
    DateTime CorrectedAtUtc);

public sealed record DocumentReview(
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
    IReadOnlyList<DocumentReviewCorrection> Corrections,
    string? MachineResultJson);

public sealed record StartReviewRequest();

public sealed record AddCorrectionRequest(
    string FieldName,
    string? OriginalValue,
    string? CorrectedValue);

public sealed record CompleteReviewRequest(
    DocumentReviewDecision Decision,
    string? Reason);
