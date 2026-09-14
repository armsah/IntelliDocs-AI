namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentAnalysisRequest(
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> Content,
    DocumentAnalysisModel Model,
    IReadOnlyList<string>? QueryFields = null);
