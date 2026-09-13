namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentLine(
    string Content,
    IReadOnlyList<float> Polygon);
