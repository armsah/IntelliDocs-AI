namespace IntelliDocs.Core.Documents;

public sealed class DocumentAnalysisRecord
{
    private DocumentAnalysisRecord()
    {
    }

    public DocumentAnalysisRecord(
        Guid documentId,
        string modelId,
        string? modelVersion,
        string resultJson,
        DateTime analyzedAtUtc)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Document ID is required.",
                nameof(documentId));
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException(
                "Model ID is required.",
                nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(resultJson))
        {
            throw new ArgumentException(
                "Analysis result JSON is required.",
                nameof(resultJson));
        }

        DocumentId = documentId;
        ModelId = modelId.Trim();
        ModelVersion = string.IsNullOrWhiteSpace(modelVersion)
            ? null
            : modelVersion.Trim();
        ResultJson = resultJson;
        AnalyzedAtUtc = analyzedAtUtc;
    }

    public Guid DocumentId { get; private set; }

    public string ModelId { get; private set; } = string.Empty;

    public string? ModelVersion { get; private set; }

    public string ResultJson { get; private set; } = string.Empty;

    public DateTime AnalyzedAtUtc { get; private set; }
}