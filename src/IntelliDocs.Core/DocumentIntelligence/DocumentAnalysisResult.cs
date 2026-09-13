namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentAnalysisResult(
    string ModelId,
    string? ModelVersion,
    string Content,
    IReadOnlyList<DocumentPage> Pages,
    IReadOnlyList<DocumentExtractedField> Fields,
    IReadOnlyList<DocumentTable> Tables);
