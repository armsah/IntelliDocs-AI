namespace IntelliDocs.Core.DocumentClassification;

public sealed record DocumentClassificationResult(
    string ClassifierId,
    string DocumentType,
    double Confidence);
