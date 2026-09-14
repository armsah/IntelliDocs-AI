namespace IntelliDocs.Core.DocumentClassification;

public interface IDocumentClassifier
{
    Task<DocumentClassificationResult> ClassifyAsync(
        DocumentClassificationRequest request,
        CancellationToken cancellationToken = default);
}
