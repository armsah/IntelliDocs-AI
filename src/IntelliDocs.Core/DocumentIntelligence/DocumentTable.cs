namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentTable(
    int RowCount,
    int ColumnCount,
    IReadOnlyList<DocumentTableCell> Cells,
    IReadOnlyList<DocumentBoundingRegion> BoundingRegions);
