namespace IntelliDocs.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string ServiceUri { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "documents";
}