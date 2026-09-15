using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using IntelliDocs.Api.Models;
using IntelliDocs.Core.Documents;
using IntelliDocs.Infrastructure.Persistence;
using IntelliDocs.Core.Messaging;
using IntelliDocs.IntegrationTests.Messaging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IntelliDocs.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;

namespace IntelliDocs.IntegrationTests.Api;

public sealed class DocumentLifecycleTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentLifecycleTests(
        WebApplicationFactory<Program> factory)
    {
        _factory =
            factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    "ConnectionStrings:PostgreSql",
                    "Host=127.0.0.1;Port=5433;" +
                    "Database=intellidocs;" +
                    "Username=intellidocs;" +
                    "Password=intellidocs_dev");

                builder.ConfigureTestServices(services =>
                {
                    services
                        .AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme =
                                TestAuthenticationHandler.AuthenticationScheme;

                            options.DefaultChallengeScheme =
                                TestAuthenticationHandler.AuthenticationScheme;
                        })
                    .AddScheme<
                        AuthenticationSchemeOptions,
                        TestAuthenticationHandler>(
                        TestAuthenticationHandler.AuthenticationScheme,
                        _ => { });
                });

                builder.UseSetting(
                    "ConnectionStrings:BlobStorage",
                    "UseDevelopmentStorage=true");

                builder.UseSetting(
                    "BlobStorage:ContainerName",
                    "documents");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDocumentProcessingPublisher>();

                    services.AddSingleton<FakeDocumentProcessingPublisher>();

                    services.AddSingleton<IDocumentProcessingPublisher>(
                        sp =>
                            sp.GetRequiredService<
                                FakeDocumentProcessingPublisher>());
                });
            });
    }

    [Fact]
    public async Task UploadAndLifecycle_ArePersistedInPostgreSql()
    {
        await ResetDatabaseAsync();

        using var client = _factory.CreateClient();

        using var documentContent =
            new ByteArrayContent(
                Encoding.UTF8.GetBytes(
                    "IntelliDocs P1 integration document"));

        documentContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/pdf");

        using var form = new MultipartFormDataContent();

        form.Add(
            new StringContent("tenant-integration"),
            "tenantId");

        form.Add(
            documentContent,
            "file",
            "invoice-p1.pdf");

        var uploadResponse =
            await client.PostAsync(
                "/api/v1/documents",
                form);

        Assert.Equal(
            HttpStatusCode.Created,
            uploadResponse.StatusCode);

        var uploaded =
            await uploadResponse.Content
                .ReadFromJsonAsync<DocumentJobResponse>(
                    JsonOptions());

        Assert.NotNull(uploaded);
        Assert.NotEqual(Guid.Empty, uploaded.DocumentId);
        Assert.Equal(
            DocumentStatus.Queued,
            uploaded.ProcessingStatus);
        Assert.Equal(
            "tenant-integration",
            uploaded.TenantId);
        Assert.Equal(
            "invoice-p1.pdf",
            uploaded.OriginalFileName);
        Assert.Equal(64, uploaded.Sha256.Length);

        var expectedLifecycle = new[]
        {
            DocumentStatus.Processing,
            DocumentStatus.Extracted,
            DocumentStatus.Validating,
            DocumentStatus.Approved,
            DocumentStatus.Publishing,
            DocumentStatus.Completed
        };

        foreach (var nextStatus in expectedLifecycle)
        {
            var transitionResponse =
                await client.PostAsJsonAsync(
                    $"/api/v1/documents/" +
                    $"{uploaded.DocumentId}/transitions",
                    new TransitionDocumentRequest(
                        nextStatus,
                        "integration-test",
                        StageFor(nextStatus),
                        $"Transition to {nextStatus}."));

            Assert.Equal(
                HttpStatusCode.OK,
                transitionResponse.StatusCode);
        }

        var getResponse =
            await client.GetAsync(
                $"/api/v1/documents/{uploaded.DocumentId}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var completed =
            await getResponse.Content
                .ReadFromJsonAsync<DocumentJobResponse>(
                    JsonOptions());

        Assert.NotNull(completed);
        Assert.Equal(
            DocumentStatus.Completed,
            completed.ProcessingStatus);
        Assert.NotNull(completed.CompletedAtUtc);
        Assert.Equal(
            8,
            completed.Transitions.Count);

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IntelliDocsDbContext>();

        var publisher =
            scope.ServiceProvider
                .GetRequiredService<
                    FakeDocumentProcessingPublisher>();

        publisher.Clear();

        var persisted =
            await dbContext.DocumentJobs
                .AsNoTracking()
                .Include(x => x.Transitions)
                .SingleAsync(
                    x =>
                        x.DocumentId ==
                        uploaded.DocumentId);

        Assert.Equal(
            DocumentStatus.Completed,
            persisted.ProcessingStatus);

        Assert.Equal(
            8,
            persisted.Transitions.Count);
    }

    [Fact]
    public async Task Upload_PersistsDocumentInBlobStorage()
    {
        await ResetDatabaseAsync();

        using var client = _factory.CreateClient();

        var bytes =
            Encoding.UTF8.GetBytes(
                "IntelliDocs P2 blob integration document");

        using var documentContent =
            new ByteArrayContent(bytes);

        documentContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/pdf");

        using var form =
            new MultipartFormDataContent();

        form.Add(
            new StringContent("tenant-blob"),
            "tenantId");

        form.Add(
            documentContent,
            "file",
            "invoice-p2.pdf");

        var uploadResponse =
            await client.PostAsync(
                "/api/v1/documents",
                form);

        Assert.Equal(
            HttpStatusCode.Created,
            uploadResponse.StatusCode);

        var uploaded =
            await uploadResponse.Content
                .ReadFromJsonAsync<DocumentJobResponse>(
                    JsonOptions());

        Assert.NotNull(uploaded);

        Assert.Equal(
            DocumentStatus.Queued,
            uploaded.ProcessingStatus);

        using var scope =
            _factory.Services.CreateScope();

        var blobServiceClient =
            scope.ServiceProvider
                .GetRequiredService<BlobServiceClient>();

        var container =
            blobServiceClient.GetBlobContainerClient(
                "documents");

        var blobName =
            $"tenant-blob/" +
            $"{uploaded.DocumentId:N}/" +
            "invoice-p2.pdf";

        var blob =
            container.GetBlobClient(blobName);

        Assert.True(
            await blob.ExistsAsync());

        var properties =
            await blob.GetPropertiesAsync();

        Assert.Equal(
            "application/pdf",
            properties.Value.ContentType);

        Assert.Equal(
            uploaded.DocumentId.ToString(),
            properties.Value.Metadata["documentId"]);

        Assert.Equal(
            "tenant-blob",
            properties.Value.Metadata["tenantId"]);

        Assert.Equal(
            uploaded.Sha256,
            properties.Value.Metadata["sha256"]);

        var downloaded =
            await blob.DownloadContentAsync();

        Assert.Equal(
            bytes,
            downloaded.Value.Content.ToArray());

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IntelliDocsDbContext>();

        var persisted =
            await dbContext.DocumentJobs
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.DocumentId ==
                        uploaded.DocumentId);

        Assert.Equal(
            DocumentStatus.Queued,
            persisted.ProcessingStatus);

        Assert.Equal(
            uploaded.Sha256,
            persisted.Sha256);

        Assert.Equal(
            blob.Uri.ToString(),
            persisted.OriginalStorageUri);
    }

    [Fact]
    public async Task DuplicateContentWithinTenant_ReturnsConflict()
    {
        await ResetDatabaseAsync();

        using var client = _factory.CreateClient();

        var bytes =
            Encoding.UTF8.GetBytes(
                "IntelliDocs P2 duplicate document");

        using var firstResponse =
            await UploadDocumentAsync(
                client,
                "tenant-duplicate",
                "first.pdf",
                bytes);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<DocumentJobResponse>(
                    JsonOptions());

        Assert.NotNull(first);

        using var duplicateResponse =
            await UploadDocumentAsync(
                client,
                "tenant-duplicate",
                "second.pdf",
                bytes);

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);

        using var duplicateJson =
            JsonDocument.Parse(
                await duplicateResponse.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            first.DocumentId,
            duplicateJson.RootElement
                .GetProperty("documentId")
                .GetGuid());

        Assert.Equal(
            first.Sha256,
            duplicateJson.RootElement
                .GetProperty("sha256")
                .GetString());

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IntelliDocsDbContext>();

        var jobCount =
            await dbContext.DocumentJobs
                .CountAsync(
                    x =>
                        x.TenantId == "tenant-duplicate" &&
                        x.Sha256 == first.Sha256);

        Assert.Equal(
            1,
            jobCount);

        var blobServiceClient =
            scope.ServiceProvider
                .GetRequiredService<BlobServiceClient>();

        var container =
            blobServiceClient.GetBlobContainerClient(
                "documents");

        var blobCount = 0;

        await foreach (
            var _ in container.GetBlobsAsync(
                Azure.Storage.Blobs.Models.BlobTraits.None,
                Azure.Storage.Blobs.Models.BlobStates.None,
                "tenant-duplicate/",
                CancellationToken.None))
        {
            blobCount++;
        }

        Assert.Equal(
            1,
            blobCount);
    }

    [Fact]
    public async Task SameContentAcrossDifferentTenants_IsAllowed()
    {
        await ResetDatabaseAsync();

        using var client = _factory.CreateClient();

        var bytes =
            Encoding.UTF8.GetBytes(
                "IntelliDocs P2 shared tenant document");

        using var firstResponse =
            await UploadDocumentAsync(
                client,
                "tenant-alpha",
                "shared-alpha.pdf",
                bytes);

        using var secondResponse =
            await UploadDocumentAsync(
                client,
                "tenant-beta",
                "shared-beta.pdf",
                bytes);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<DocumentJobResponse>(
                    JsonOptions());

        var second =
            await secondResponse.Content
                .ReadFromJsonAsync<DocumentJobResponse>(
                    JsonOptions());

        Assert.NotNull(first);
        Assert.NotNull(second);

        Assert.NotEqual(
            first.DocumentId,
            second.DocumentId);

        Assert.Equal(
            first.Sha256,
            second.Sha256);

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IntelliDocsDbContext>();

        var jobCount =
            await dbContext.DocumentJobs
                .CountAsync(
                    x => x.Sha256 == first.Sha256);

        Assert.Equal(
            2,
            jobCount);

        var blobServiceClient =
            scope.ServiceProvider
                .GetRequiredService<BlobServiceClient>();

        var container =
            blobServiceClient.GetBlobContainerClient(
                "documents");

        var blobCount = 0;

        await foreach (
            var _ in container.GetBlobsAsync(
                Azure.Storage.Blobs.Models.BlobTraits.None,
                Azure.Storage.Blobs.Models.BlobStates.None,
                null,
                CancellationToken.None))
        {
            blobCount++;
        }

        Assert.Equal(
            2,
            blobCount);
    }

    [Fact]
    public async Task ConcurrentDuplicateContentWithinTenant_CreatesOneDocument()
    {
        await ResetDatabaseAsync();

        using var client = _factory.CreateClient();

        var bytes =
            Encoding.UTF8.GetBytes(
                "IntelliDocs P2 concurrent duplicate");

        var firstTask =
            UploadDocumentAsync(
                client,
                "tenant-concurrent",
                "first.pdf",
                bytes);

        var secondTask =
            UploadDocumentAsync(
                client,
                "tenant-concurrent",
                "second.pdf",
                bytes);

        var responses =
            await Task.WhenAll(
                firstTask,
                secondTask);

        try
        {
            Assert.Equal(
                1,
                responses.Count(
                    x =>
                        x.StatusCode ==
                        HttpStatusCode.Created));

            Assert.Equal(
                1,
                responses.Count(
                    x =>
                        x.StatusCode ==
                        HttpStatusCode.Conflict));

            using var scope =
                _factory.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<IntelliDocsDbContext>();

            var jobCount =
                await dbContext.DocumentJobs
                    .CountAsync(
                        x =>
                            x.TenantId ==
                            "tenant-concurrent");

            Assert.Equal(
                1,
                jobCount);

            var blobServiceClient =
                scope.ServiceProvider
                    .GetRequiredService<BlobServiceClient>();

            var container =
                blobServiceClient.GetBlobContainerClient(
                    "documents");

            var blobCount = 0;

            await foreach (
                var _ in container.GetBlobsAsync(
                    Azure.Storage.Blobs.Models.BlobTraits.None,
                    Azure.Storage.Blobs.Models.BlobStates.None,
                    "tenant-concurrent/",
                    CancellationToken.None))
            {
                blobCount++;
            }

            Assert.Equal(
                1,
                blobCount);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Upload_QueuesInitialLayoutExtractionMessage()
    {
        await ResetDatabaseAsync();

        using var client =
            _factory.CreateClient();

        var bytes =
            Encoding.UTF8.GetBytes(
                "IntelliDocs P5 queue integration document");

        using var response =
            await UploadDocumentAsync(
                client,
                "tenant-queue",
                "queue-document.pdf",
                bytes);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var uploaded =
            await response.Content
                .ReadFromJsonAsync<DocumentJobResponse>(
                    JsonOptions());

        Assert.NotNull(uploaded);

        Assert.Equal(
            DocumentStatus.Queued,
            uploaded.ProcessingStatus);

        using var scope =
            _factory.Services.CreateScope();

        var publisher =
            scope.ServiceProvider
                .GetRequiredService<
                    FakeDocumentProcessingPublisher>();

        var message =
            Assert.Single(
                publisher.Messages);

        Assert.Equal(
            uploaded.DocumentId,
            message.DocumentId);

        Assert.Equal(
            "tenant-queue",
            message.TenantId);

        Assert.Equal(
            "layout-extraction",
            message.ProcessingStage);

        Assert.Equal(
            0,
            message.RedriveCount);

        Assert.Equal(
            $"{uploaded.DocumentId:N}:layout-extraction",
            message.IdempotencyKey);

        Assert.Equal(
            $"{uploaded.DocumentId:N}:layout-extraction:r0",
            message.MessageId);
    }

    [Fact]
    public async Task InvalidTransition_ReturnsConflict()
    {
        await ResetDatabaseAsync();

        using var client = _factory.CreateClient();

        using var content =
            new ByteArrayContent(
                Encoding.UTF8.GetBytes(
                    "invalid transition"));

        using var form =
            new MultipartFormDataContent();

        form.Add(
            new StringContent("tenant-integration"),
            "tenantId");

        form.Add(
            content,
            "file",
            "invalid-transition.pdf");

        var uploadResponse =
            await client.PostAsync(
                "/api/v1/documents",
                form);

        Assert.Equal(
            HttpStatusCode.Created,
            uploadResponse.StatusCode);

        var uploaded =
            await uploadResponse.Content
                .ReadFromJsonAsync<DocumentJobResponse>(
                    JsonOptions());

        Assert.NotNull(uploaded);

        var response =
            await client.PostAsJsonAsync(
                $"/api/v1/documents/" +
                $"{uploaded.DocumentId}/transitions",
                new TransitionDocumentRequest(
                    DocumentStatus.Completed,
                    "integration-test",
                    "invalid",
                    null));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope =
        _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IntelliDocsDbContext>();

        await dbContext.Database.MigrateAsync();
        await dbContext.DocumentReviewCorrections
            .ExecuteDeleteAsync();

        await dbContext.DocumentReviews
            .ExecuteDeleteAsync();

        await dbContext.DocumentAnalysisRecords
            .ExecuteDeleteAsync();

        await dbContext.DocumentJobTransitions
            .ExecuteDeleteAsync();

        await dbContext.DocumentJobs
            .ExecuteDeleteAsync();

        var blobServiceClient =
            scope.ServiceProvider
                .GetRequiredService<BlobServiceClient>();

        var container =
            blobServiceClient.GetBlobContainerClient(
                "documents");

        await container.DeleteIfExistsAsync();
    }

    private static async Task<HttpResponseMessage>
    UploadDocumentAsync(
        HttpClient client,
        string tenantId,
        string fileName,
        byte[] bytes)
    {
        using var content =
            new ByteArrayContent(bytes);

        content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/pdf");

        using var form =
            new MultipartFormDataContent();

        form.Add(
            new StringContent(tenantId),
            "tenantId");

        form.Add(
            content,
            "file",
            fileName);

        return await client.PostAsync(
            "/api/v1/documents",
            form);
    }

    private static string StageFor(
        DocumentStatus status)
    {
        return status switch
        {
            DocumentStatus.Queued => "queue",
            DocumentStatus.Processing => "processing",
            DocumentStatus.Extracted => "extraction",
            DocumentStatus.Validating => "validation",
            DocumentStatus.Approved => "approval",
            DocumentStatus.Publishing => "publishing",
            DocumentStatus.Completed => "publishing",
            _ => "integration-test"
        };
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options =
            new JsonSerializerOptions(
                JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        options.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());

        return options;
    }
}
