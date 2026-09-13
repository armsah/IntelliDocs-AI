using IntelliDocs.Core.DocumentIntelligence;

namespace IntelliDocs.UnitTests.DocumentIntelligence;

internal sealed class FakeDocumentIntelligenceProvider
    : IDocumentIntelligenceProvider
{
    public DocumentAnalysisRequest? LastRequest { get; private set; }

    public Task<DocumentAnalysisResult> AnalyzeAsync(
        DocumentAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        LastRequest = request;

        var modelId = request.Model switch
        {
            DocumentAnalysisModel.Invoice => "fake-prebuilt-invoice",
            DocumentAnalysisModel.Layout => "fake-prebuilt-layout",
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        var result = new DocumentAnalysisResult(
            modelId,
            "fake-v1",
            "Synthetic invoice content",
            [
                new DocumentPage(
                    1,
                    8.5f,
                    11f,
                    "inch",
                    [
                        new DocumentLine(
                            "Invoice INV-1001",
                            [1f, 1f, 4f, 1f, 4f, 1.5f, 1f, 1.5f])
                    ])
            ],
            [
                new DocumentExtractedField(
                    "InvoiceId",
                    "INV-1001",
                    0.99,
                    [
                        new DocumentBoundingRegion(
                            1,
                            [1f, 1f, 4f, 1f, 4f, 1.5f, 1f, 1.5f])
                    ])
            ],
            []);

        return Task.FromResult(result);
    }
}
