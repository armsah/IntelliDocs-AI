using IntelliDocs.Core.DocumentClassification;
using IntelliDocs.Core.Validation;

namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentProcessingResult(
    DocumentClassificationResult Classification,
    DocumentAnalysisResult Analysis,
    DocumentValidationResult Validation);
