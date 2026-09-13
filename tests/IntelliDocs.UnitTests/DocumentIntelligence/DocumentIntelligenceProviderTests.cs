using IntelliDocs.Core.DocumentIntelligence;
using IntelliDocs.Infrastructure.DocumentIntelligence;

namespace IntelliDocs.UnitTests.DocumentIntelligence;

public sealed class DocumentIntelligenceProviderTests
{
    [Theory]
    [InlineData(DocumentAnalysisModel.Layout, "prebuilt-layout")]
    [InlineData(DocumentAnalysisModel.Invoice, "prebuilt-invoice")]
    public void GetModelId_MapsSupportedModels(
        DocumentAnalysisModel model,
        string expectedModelId)
    {
        var actual = AzureDocumentIntelligenceProvider.GetModelId(model);

        Assert.Equal(expectedModelId, actual);
    }

    [Fact]
    public void GetModelId_RejectsUnsupportedModel()
    {
        var invalidModel = (DocumentAnalysisModel)999;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => AzureDocumentIntelligenceProvider.GetModelId(invalidModel));
    }

    [Fact]
    public async Task FakeProvider_ReturnsDeterministicNormalizedResult()
    {
        var provider = new FakeDocumentIntelligenceProvider();

        var request = new DocumentAnalysisRequest(
            "invoice.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3 },
            DocumentAnalysisModel.Invoice);

        var result = await provider.AnalyzeAsync(request);

        Assert.Same(request, provider.LastRequest);
        Assert.Equal("fake-prebuilt-invoice", result.ModelId);
        Assert.Equal("fake-v1", result.ModelVersion);
        Assert.Equal("Synthetic invoice content", result.Content);

        var page = Assert.Single(result.Pages);
        Assert.Equal(1, page.PageNumber);
        Assert.Single(page.Lines);

        var field = Assert.Single(result.Fields);
        Assert.Equal("InvoiceId", field.Name);
        Assert.Equal("INV-1001", field.Content);
        Assert.Equal(0.99, field.Confidence);

        var region = Assert.Single(field.BoundingRegions);
        Assert.Equal(1, region.PageNumber);
        Assert.Equal(8, region.Polygon.Count);

        Assert.Empty(result.Tables);
    }
}
