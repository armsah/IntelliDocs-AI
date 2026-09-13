using Azure.AI.DocumentIntelligence;
using IntelliDocs.Infrastructure.DocumentIntelligence;

namespace IntelliDocs.UnitTests.DocumentIntelligence;

public sealed class AzureDocumentIntelligenceMapperTests
{
    [Fact]
    public void MapResult_MapsPagesFieldsTablesConfidenceAndProvenance()
    {
        var fieldRegion =
            DocumentIntelligenceModelFactory.BoundingRegion(
                1,
                new float[]
                {
                    1f, 1f,
                    4f, 1f,
                    4f, 1.5f,
                    1f, 1.5f
                });

        var tableRegion =
            DocumentIntelligenceModelFactory.BoundingRegion(
                1,
                new float[]
                {
                    0.5f, 5f,
                    7.5f, 5f,
                    7.5f, 7f,
                    0.5f, 7f
                });

        var line =
            DocumentIntelligenceModelFactory.DocumentLine(
                "Invoice INV-1001",
                new float[]
                {
                    1f, 1f,
                    4f, 1f,
                    4f, 1.5f,
                    1f, 1.5f
                },
                Array.Empty<DocumentSpan>());

        var page =
            DocumentIntelligenceModelFactory.DocumentPage(
                1,
                null,
                8.5f,
                11f,
                null,
                Array.Empty<DocumentSpan>(),
                Array.Empty<DocumentWord>(),
                Array.Empty<DocumentSelectionMark>(),
                new[] { line },
                Array.Empty<DocumentBarcode>(),
                Array.Empty<DocumentFormula>());

        var invoiceIdField =
            DocumentIntelligenceModelFactory.DocumentField(
                new DocumentFieldType("string"),
                "INV-1001",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                Array.Empty<DocumentField>(),
                DocumentIntelligenceModelFactory.DocumentFieldDictionary(
                    new Dictionary<string, DocumentField>()),
                null,
                null,
                null,
                Array.Empty<string>(),
                "INV-1001",
                new[] { fieldRegion },
                Array.Empty<DocumentSpan>(),
                0.98f);

        var fields =
            DocumentIntelligenceModelFactory.DocumentFieldDictionary(
                new Dictionary<string, DocumentField>
                {
                    ["InvoiceId"] = invoiceIdField
                });

        var analyzedDocument =
            DocumentIntelligenceModelFactory.AnalyzedDocument(
                "invoice",
                Array.Empty<BoundingRegion>(),
                Array.Empty<DocumentSpan>(),
                fields,
                0.97f);

        var tableCell =
            DocumentIntelligenceModelFactory.DocumentTableCell(
                null,
                0,
                0,
                1,
                1,
                "Widget A",
                new[] { tableRegion },
                Array.Empty<DocumentSpan>(),
                Array.Empty<string>());

        var table =
            DocumentIntelligenceModelFactory.DocumentTable(
                1,
                1,
                new[] { tableCell },
                new[] { tableRegion },
                Array.Empty<DocumentSpan>(),
                null,
                Array.Empty<DocumentFootnote>());

        var azureResult =
            DocumentIntelligenceModelFactory.AnalyzeResult(
                "2024-11-30",
                "prebuilt-invoice",
                null,
                "Invoice INV-1001" + Environment.NewLine + "Widget A",
                new[] { page },
                Array.Empty<DocumentParagraph>(),
                new[] { table },
                Array.Empty<DocumentFigure>(),
                Array.Empty<DocumentSection>(),
                Array.Empty<DocumentKeyValuePair>(),
                Array.Empty<DocumentStyle>(),
                Array.Empty<DocumentLanguage>(),
                new[] { analyzedDocument },
                Array.Empty<DocumentIntelligenceWarning>());

        var result =
            AzureDocumentIntelligenceProvider.MapResult(
                "prebuilt-invoice",
                azureResult);

        Assert.Equal("prebuilt-invoice", result.ModelId);
        Assert.Null(result.ModelVersion);
        Assert.Contains("Invoice INV-1001", result.Content);

        var mappedPage = Assert.Single(result.Pages);

        Assert.Equal(1, mappedPage.PageNumber);
        Assert.Equal(8.5f, mappedPage.Width);
        Assert.Equal(11f, mappedPage.Height);

        var mappedLine = Assert.Single(mappedPage.Lines);

        Assert.Equal("Invoice INV-1001", mappedLine.Content);
        Assert.Equal(8, mappedLine.Polygon.Count);

        var mappedField = Assert.Single(result.Fields);

        Assert.Equal("InvoiceId", mappedField.Name);
        Assert.Equal("INV-1001", mappedField.Content);
        Assert.NotNull(mappedField.Confidence);
        Assert.InRange(
            mappedField.Confidence.Value,
            0.979,
            0.981);

        var mappedFieldRegion =
            Assert.Single(mappedField.BoundingRegions);

        Assert.Equal(1, mappedFieldRegion.PageNumber);
        Assert.Equal(8, mappedFieldRegion.Polygon.Count);

        var mappedTable = Assert.Single(result.Tables);

        Assert.Equal(1, mappedTable.RowCount);
        Assert.Equal(1, mappedTable.ColumnCount);

        var mappedCell = Assert.Single(mappedTable.Cells);

        Assert.Equal(0, mappedCell.RowIndex);
        Assert.Equal(0, mappedCell.ColumnIndex);
        Assert.Equal(1, mappedCell.RowSpan);
        Assert.Equal(1, mappedCell.ColumnSpan);
        Assert.Equal("Widget A", mappedCell.Content);

        var mappedCellRegion =
            Assert.Single(mappedCell.BoundingRegions);

        Assert.Equal(1, mappedCellRegion.PageNumber);
        Assert.Equal(8, mappedCellRegion.Polygon.Count);

        var mappedTableRegion =
            Assert.Single(mappedTable.BoundingRegions);

        Assert.Equal(1, mappedTableRegion.PageNumber);
    }
}
