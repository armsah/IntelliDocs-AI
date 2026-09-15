namespace IntelliDocs.Core.Validation;

public sealed record ValidationIssue(
    string Code,
    string Message,
    ValidationSeverity Severity,
    string? FieldName = null);
