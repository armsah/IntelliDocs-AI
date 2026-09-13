namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentPage(
    int PageNumber,
    float? Width,
    float? Height,
    string? Unit,
    IReadOnlyList<DocumentLine> Lines);
