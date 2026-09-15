using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using IntelliDocs.Core.DocumentClassification;
using Microsoft.Extensions.Options;

namespace IntelliDocs.Infrastructure.DocumentIntelligence;

public sealed class AzureDocumentClassifier
    : IDocumentClassifier
{
    private readonly DocumentIntelligenceClient _client;
    private readonly string _classifierId;

    public AzureDocumentClassifier(
        IOptions<DocumentIntelligenceOptions> options,
        TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException(
                "Document Intelligence endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.ClassifierId))
        {
            throw new InvalidOperationException(
                "Document Intelligence classifier ID is not configured.");
        }

        _classifierId = settings.ClassifierId.Trim();

        var endpoint = new Uri(settings.Endpoint);

        _client = string.IsNullOrWhiteSpace(settings.ApiKey)
            ? new DocumentIntelligenceClient(
                endpoint,
                credential)
            : new DocumentIntelligenceClient(
                endpoint,
                new AzureKeyCredential(settings.ApiKey));
    }

    public async Task<DocumentClassificationResult> ClassifyAsync(
        DocumentClassificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Content.IsEmpty)
        {
            throw new ArgumentException(
                "Document content cannot be empty.",
                nameof(request));
        }

        var options =
            new ClassifyDocumentOptions(
                _classifierId,
                BinaryData.FromBytes(request.Content));

        var operation =
            await _client.ClassifyDocumentAsync(
                WaitUntil.Completed,
                options,
                cancellationToken);

        var bestDocument =
            operation.Value.Documents
                .OrderByDescending(
                    document => document.Confidence)
                .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Document classifier returned no classification.");

        if (string.IsNullOrWhiteSpace(
                bestDocument.DocumentType))
        {
            throw new InvalidOperationException(
                "Document classifier returned an empty document type.");
        }

        return new DocumentClassificationResult(
            _classifierId,
            bestDocument.DocumentType,
            bestDocument.Confidence);
    }
}
