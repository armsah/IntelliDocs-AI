namespace IntelliDocs.Infrastructure.DocumentIntelligence;

public sealed class DocumentIntelligenceOptions
{
    public const string SectionName = "DocumentIntelligence";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ClassifierId { get; set; } =
        "intellidocs-p6-classifier-v1";
}
