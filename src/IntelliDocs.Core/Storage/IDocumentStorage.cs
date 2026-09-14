namespace IntelliDocs.Core.Storage;

public interface IDocumentStorage
{
    Task<DocumentStorageResult> StoreAsync(
        DocumentStorageRequest request,
        CancellationToken cancellationToken = default);

    Task<StoredDocumentContent> OpenReadAsync(
        string storageUri,
        CancellationToken cancellationToken = default);
}
