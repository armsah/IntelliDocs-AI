namespace IntelliDocs.Core.Validation;

public sealed record NormalizedDocumentField(
    string Name,
    string? RawValue,
    string? NormalizedValue,
    double? Confidence);
