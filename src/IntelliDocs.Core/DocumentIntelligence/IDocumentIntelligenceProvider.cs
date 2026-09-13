namespace IntelliDocs.Core.DocumentIntelligence;

public interface IDocumentIntelligenceProvider
{
    Task<DocumentAnalysisResult> AnalyzeAsync(
        DocumentAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
