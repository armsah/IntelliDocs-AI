namespace IntelliDocs.Core.Storage;

public interface IDocumentStorage
{
    Task<DocumentStorageResult> StoreAsync(
        DocumentStorageRequest request,
        CancellationToken cancellationToken = default);
}
