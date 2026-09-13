using Azure;
using Azure.AI.DocumentIntelligence;
using IntelliDocs.Core.DocumentIntelligence;
using Microsoft.Extensions.Options;

namespace IntelliDocs.Infrastructure.DocumentIntelligence;

public sealed class AzureDocumentIntelligenceProvider
    : IDocumentIntelligenceProvider
{
    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentIntelligenceProvider(
        IOptions<DocumentIntelligenceOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException(
                "Document Intelligence endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException(
                "Document Intelligence API key is not configured.");
        }

        _client = new DocumentIntelligenceClient(
            new Uri(settings.Endpoint),
            new AzureKeyCredential(settings.ApiKey));
    }

    public async Task<DocumentAnalysisResult> AnalyzeAsync(
        DocumentAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Content.IsEmpty)
        {
            throw new ArgumentException(
                "Document content cannot be empty.",
                nameof(request));
        }

        var modelId = GetModelId(request.Model);

        var operation =
            await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                modelId,
                BinaryData.FromBytes(request.Content),
                cancellationToken);

        return MapResult(
            modelId,
            operation.Value);
    }

    internal static string GetModelId(DocumentAnalysisModel model)
    {
        return model switch
        {
            DocumentAnalysisModel.Layout => "prebuilt-layout",
            DocumentAnalysisModel.Invoice => "prebuilt-invoice",
            _ => throw new ArgumentOutOfRangeException(
                nameof(model),
                model,
                "Unsupported document analysis model.")
        };
    }

    internal static DocumentAnalysisResult MapResult(
        string modelId,
        AnalyzeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var pages =
            result.Pages
                .Select(page =>
                    new IntelliDocs.Core.DocumentIntelligence.DocumentPage(
                        page.PageNumber,
                        page.Width,
                        page.Height,
                        page.Unit.ToString(),
                        page.Lines
                            .Select(line =>
                                new IntelliDocs.Core.DocumentIntelligence.DocumentLine(
                                    line.Content,
                                    MapPolygon(line.Polygon)))
                            .ToArray()))
                .ToArray();

        var fields =
            result.Documents
                .SelectMany(document => document.Fields)
                .Select(entry =>
                    new DocumentExtractedField(
                        entry.Key,
                        entry.Value.Content,
                        entry.Value.Confidence,
                        entry.Value.BoundingRegions
                            .Select(MapBoundingRegion)
                            .ToArray()))
                .ToArray();

        var tables =
            result.Tables
                .Select(table =>
                    new IntelliDocs.Core.DocumentIntelligence.DocumentTable(
                        table.RowCount,
                        table.ColumnCount,
                        table.Cells
                            .Select(cell =>
                                new IntelliDocs.Core.DocumentIntelligence.DocumentTableCell(
                                    cell.RowIndex,
                                    cell.ColumnIndex,
                                    cell.RowSpan ?? 1,
                                    cell.ColumnSpan ?? 1,
                                    cell.Content,
                                    cell.BoundingRegions
                                        .Select(MapBoundingRegion)
                                        .ToArray()))
                            .ToArray(),
                        table.BoundingRegions
                            .Select(MapBoundingRegion)
                            .ToArray()))
                .ToArray();

        return new DocumentAnalysisResult(
            modelId,
            null,
            result.Content ?? string.Empty,
            pages,
            fields,
            tables);
    }

    private static DocumentBoundingRegion MapBoundingRegion(
        BoundingRegion region)
    {
        return new DocumentBoundingRegion(
            region.PageNumber,
            MapPolygon(region.Polygon));
    }

    private static IReadOnlyList<float> MapPolygon(
        IReadOnlyList<float> polygon)
    {
        return polygon.ToArray();
    }
}