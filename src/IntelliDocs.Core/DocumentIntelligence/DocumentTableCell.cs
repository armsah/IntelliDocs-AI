namespace IntelliDocs.Core.DocumentIntelligence;

public sealed record DocumentTableCell(
    int RowIndex,
    int ColumnIndex,
    int RowSpan,
    int ColumnSpan,
    string Content,
    IReadOnlyList<DocumentBoundingRegion> BoundingRegions);
