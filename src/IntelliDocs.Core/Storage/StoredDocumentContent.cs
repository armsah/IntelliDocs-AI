namespace IntelliDocs.Core.Storage;

public sealed record StoredDocumentContent(
    Stream Content,
    string ContentType,
    long ContentLength);