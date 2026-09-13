using System.Globalization;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using IntelliDocs.Core.Storage;
using Microsoft.Extensions.Options;

namespace IntelliDocs.Infrastructure.Storage;

public sealed class AzureBlobDocumentStorage : IDocumentStorage
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobDocumentStorage(
        BlobServiceClient blobServiceClient,
        IOptions<BlobStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        ArgumentNullException.ThrowIfNull(options);

        var containerName = options.Value.ContainerName;

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException(
                "Blob storage container name is not configured.");
        }

        _containerClient =
            blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<DocumentStorageResult> StoreAsync(
        DocumentStorageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        if (!request.Content.CanRead)
        {
            throw new ArgumentException(
                "Document content stream must be readable.",
                nameof(request));
        }

        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blobName = BuildBlobName(request);
        var blobClient = _containerClient.GetBlobClient(blobName);

        var metadata = new Dictionary<string, string>
        {
            ["documentId"] =
                request.DocumentId.ToString("D", CultureInfo.InvariantCulture),
            ["tenantId"] = request.TenantId,
            ["sha256"] = request.Sha256
        };

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType =
                    string.IsNullOrWhiteSpace(request.ContentType)
                        ? "application/octet-stream"
                        : request.ContentType
            },
            Metadata = metadata,
            Conditions = new BlobRequestConditions
            {
                IfNoneMatch = ETag.All
            }
        };

        await blobClient.UploadAsync(
            request.Content,
            uploadOptions,
            cancellationToken);

        var properties =
            await blobClient.GetPropertiesAsync(
                cancellationToken: cancellationToken);

        return new DocumentStorageResult(
            blobClient.Uri.ToString(),
            blobName,
            properties.Value.ContentLength);
    }

    private static string BuildBlobName(
        DocumentStorageRequest request)
    {
        var tenantSegment =
            SanitizePathSegment(request.TenantId, "tenant");

        var fileName =
            SanitizeFileName(request.FileName);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{tenantSegment}/{request.DocumentId:N}/{fileName}");
    }

    private static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(safeName))
        {
            return "document";
        }

        return SanitizePathSegment(
            safeName,
            "document");
    }

    private static string SanitizePathSegment(
        string value,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character) ||
                character is '-' or '_' or '.')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('-');
            }
        }

        var result = builder.ToString().Trim('-', '.');

        return string.IsNullOrWhiteSpace(result)
            ? fallback
            : result;
    }
}
