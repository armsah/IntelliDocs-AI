namespace IntelliDocs.Core.Storage;

public sealed record DocumentStorageRequest(
    Guid DocumentId,
    string TenantId,
    string FileName,
    string? ContentType,
    string Sha256,
    Stream Content);
