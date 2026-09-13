using IntelliDocs.Core.Documents;

namespace IntelliDocs.Api.Models;

public sealed record DocumentTransitionResponse(
    Guid Id,
    DocumentStatus PreviousStatus,
    DocumentStatus NextStatus,
    DateTime OccurredAtUtc,
    string Actor,
    string ProcessingStage,
    string? Reason);

public sealed record DocumentJobResponse(
    Guid DocumentId,
    string TenantId,
    string OriginalFileName,
    string? OriginalStorageUri,
    string Sha256,
    string? DetectedType,
    string? AiModelId,
    string? AiModelVersion,
    DocumentStatus ProcessingStatus,
    DateTime SubmittedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyCollection<DocumentTransitionResponse> Transitions);
