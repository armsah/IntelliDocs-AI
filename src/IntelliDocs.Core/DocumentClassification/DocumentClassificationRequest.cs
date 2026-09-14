namespace IntelliDocs.Core.DocumentClassification;

public sealed record DocumentClassificationRequest(
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> Content);
