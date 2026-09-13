namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentBoundingRegion(
    int PageNumber,
    IReadOnlyList<float> Polygon);
