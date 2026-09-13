using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IntelliDocs.Api.Models;
using IntelliDocs.Core.Documents;
using IntelliDocs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
            DocumentStatus.Stored,
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
            DocumentStatus.Queued,
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

        await dbContext.DocumentJobTransitions
            .ExecuteDeleteAsync();

        await dbContext.DocumentJobs
            .ExecuteDeleteAsync();
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
