using IntelliDocs.Core.Documents;

namespace IntelliDocs.Api.Models;

public sealed record TransitionDocumentRequest(
    DocumentStatus NextStatus,
    string Actor,
    string ProcessingStage,
    string? Reason);
