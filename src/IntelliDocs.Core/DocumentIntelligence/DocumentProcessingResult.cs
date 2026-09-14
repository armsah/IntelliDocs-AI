using IntelliDocs.Core.DocumentClassification;

namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentProcessingResult(
    DocumentClassificationResult Classification,
    DocumentAnalysisResult Analysis);
