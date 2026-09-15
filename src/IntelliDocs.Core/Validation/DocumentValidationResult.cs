namespace IntelliDocs.Core.Validation;

public sealed record DocumentValidationResult(
    string DocumentType,
    double PolicyConfidence,
    DocumentRoutingDecision Decision,
    IReadOnlyList<NormalizedDocumentField> Fields,
    IReadOnlyList<ValidationIssue> Issues);
