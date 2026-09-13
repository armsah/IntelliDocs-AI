namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentExtractedField(
    string Name,
    string? Content,
    double? Confidence,
    IReadOnlyList<DocumentBoundingRegion> BoundingRegions);
