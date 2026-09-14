using IntelliDocs.Core.DocumentIntelligence;

namespace IntelliDocs.Core.DocumentClassification;

public sealed record DocumentAnalysisRoute(
    DocumentAnalysisModel Model,
    IReadOnlyList<string>? QueryFields);
