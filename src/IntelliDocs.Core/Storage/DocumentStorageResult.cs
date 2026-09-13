namespace IntelliDocs.Core.Storage;

public sealed record DocumentStorageResult(
    string StorageUri,
    string BlobName,
    long ContentLength);
