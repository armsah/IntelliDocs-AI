using System.Net;
using System.Net.Http.Json;
using IntelliDocs.Api.Models.Reviews;
using IntelliDocs.Core.Documents;
using IntelliDocs.Core.Reviews;
using IntelliDocs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntelliDocs.IntegrationTests.Api;

public sealed class DocumentReviewWorkflowTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentReviewWorkflowTests(
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
    public async Task ReviewWorkflow_CorrectionAndApproval_AreAuditable()
    {
        await ResetDatabaseAsync();

        const string machineResult =
            """{"classification":{"documentType":"invoice"},"analysis":{"modelId":"prebuilt-invoice"},"validation":{"routingDecision":"NeedsReview"}}""";

        var documentId =
            await SeedNeedsReviewDocumentAsync(
                machineResult);

        using var client = _factory.CreateClient();

        var queue =
            await client.GetFromJsonAsync<
                List<DocumentReviewQueueItemResponse>>(
                    "/api/v1/reviews",
                    JsonOptions());

        Assert.NotNull(queue);

        var queued =
            Assert.Single(
                queue,
                x => x.DocumentId == documentId);

        Assert.Equal(
            DocumentStatus.NeedsReview,
            queued.ProcessingStatus);

        using var startResponse =
            await client.PostAsJsonAsync(
                $"/api/v1/reviews/{documentId}/start",
                new StartDocumentReviewRequest(
                    "reviewer@example.com"));

        Assert.Equal(
            HttpStatusCode.OK,
            startResponse.StatusCode);

        var started =
            await startResponse.Content
                .ReadFromJsonAsync<DocumentReviewResponse>(
                    JsonOptions());

        Assert.NotNull(started);
        Assert.Equal(
            DocumentStatus.InReview,
            started.ProcessingStatus);
        Assert.Equal(
            "reviewer@example.com",
            started.Reviewer);
        AssertJsonEquivalent(
            machineResult,
            started.MachineResultJson);

        using var correctionResponse =
            await client.PostAsJsonAsync(
                $"/api/v1/reviews/{documentId}/corrections",
                new AddDocumentReviewCorrectionRequest(
                    "totalAmount",
                    "1250.00",
                    "1520.00",
                    "reviewer@example.com"));

        Assert.Equal(
            HttpStatusCode.OK,
            correctionResponse.StatusCode);

        var corrected =
            await correctionResponse.Content
                .ReadFromJsonAsync<DocumentReviewResponse>(
                    JsonOptions());

        Assert.NotNull(corrected);

        var correction =
            Assert.Single(corrected.Corrections);

        Assert.Equal(
            "totalAmount",
            correction.FieldName);
        Assert.Equal(
            "1250.00",
            correction.OriginalValue);
        Assert.Equal(
            "1520.00",
            correction.CorrectedValue);
        Assert.Equal(
            "reviewer@example.com",
            correction.Reviewer);

        using var decisionResponse =
            await client.PostAsJsonAsync(
                $"/api/v1/reviews/{documentId}/decision",
                new CompleteDocumentReviewRequest(
                    DocumentReviewDecision.Approved,
                    "reviewer@example.com",
                    "Corrected total verified."));

        Assert.Equal(
            HttpStatusCode.OK,
            decisionResponse.StatusCode);

        var completed =
            await decisionResponse.Content
                .ReadFromJsonAsync<DocumentReviewResponse>(
                    JsonOptions());

        Assert.NotNull(completed);
        Assert.Equal(
            DocumentStatus.Approved,
            completed.ProcessingStatus);
        Assert.Equal(
            DocumentReviewDecision.Approved,
            completed.Decision);
        Assert.Equal(
            "Corrected total verified.",
            completed.DecisionReason);
        Assert.NotNull(completed.DecidedAtUtc);
        AssertJsonEquivalent(
            machineResult,
            completed.MachineResultJson);

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IntelliDocsDbContext>();

        var persistedReview =
            await dbContext.DocumentReviews
                .AsNoTracking()
                .Include(x => x.Corrections)
                .SingleAsync(
                    x => x.DocumentId == documentId);

        Assert.Equal(
            DocumentReviewDecision.Approved,
            persistedReview.Decision);

        var persistedCorrection =
            Assert.Single(persistedReview.Corrections);

        Assert.Equal(
            "1520.00",
            persistedCorrection.CorrectedValue);

        var persistedAnalysis =
            await dbContext.DocumentAnalysisRecords
                .AsNoTracking()
                .SingleAsync(
                    x => x.DocumentId == documentId);

        AssertJsonEquivalent(
            machineResult,
            persistedAnalysis.ResultJson);

        var job =
            await dbContext.DocumentJobs
                .AsNoTracking()
                .Include(x => x.Transitions)
                .SingleAsync(
                    x => x.DocumentId == documentId);

        Assert.Equal(
            DocumentStatus.Approved,
            job.ProcessingStatus);

        Assert.Contains(
            job.Transitions,
            x =>
                x.PreviousStatus ==
                    DocumentStatus.NeedsReview &&
                x.NextStatus ==
                    DocumentStatus.InReview &&
                x.Actor ==
                    "reviewer@example.com" &&
                x.ProcessingStage ==
                    "human-review");

        Assert.Contains(
            job.Transitions,
            x =>
                x.PreviousStatus ==
                    DocumentStatus.InReview &&
                x.NextStatus ==
                    DocumentStatus.Approved &&
                x.Actor ==
                    "reviewer@example.com" &&
                x.ProcessingStage ==
                    "human-review");
    }

    [Fact]
    public async Task ReviewWorkflow_Rejection_IsAuditable()
    {
        await ResetDatabaseAsync();

        var documentId =
            await SeedNeedsReviewDocumentAsync(
                """{"validation":{"routingDecision":"NeedsReview"}}""");

        using var client = _factory.CreateClient();

        using var startResponse =
            await client.PostAsJsonAsync(
                $"/api/v1/reviews/{documentId}/start",
                new StartDocumentReviewRequest(
                    "reviewer@example.com"));

        Assert.Equal(
            HttpStatusCode.OK,
            startResponse.StatusCode);

        using var decisionResponse =
            await client.PostAsJsonAsync(
                $"/api/v1/reviews/{documentId}/decision",
                new CompleteDocumentReviewRequest(
                    DocumentReviewDecision.Rejected,
                    "reviewer@example.com",
                    "Document does not match source evidence."));

        Assert.Equal(
            HttpStatusCode.OK,
            decisionResponse.StatusCode);

        var completed =
            await decisionResponse.Content
                .ReadFromJsonAsync<DocumentReviewResponse>(
                    JsonOptions());

        Assert.NotNull(completed);
        Assert.Equal(
            DocumentStatus.Rejected,
            completed.ProcessingStatus);
        Assert.Equal(
            DocumentReviewDecision.Rejected,
            completed.Decision);
        Assert.Equal(
            "Document does not match source evidence.",
            completed.DecisionReason);

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IntelliDocsDbContext>();

        var review =
            await dbContext.DocumentReviews
                .AsNoTracking()
                .SingleAsync(
                    x => x.DocumentId == documentId);

        Assert.Equal(
            DocumentReviewDecision.Rejected,
            review.Decision);

        Assert.NotNull(review.DecidedAtUtc);
    }

    private async Task<Guid> SeedNeedsReviewDocumentAsync(
        string machineResultJson)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<IntelliDocsDbContext>();

        var job =
            DocumentJob.Create(
                "tenant-review",
                "review-invoice.pdf",
                new string('a', 64));

        var documentId = job.DocumentId;

        job.SetStorageUri(
            $"http://localhost/documents/{documentId:N}/review-invoice.pdf");

        job.TransitionTo(
            DocumentStatus.Stored,
            "integration-test",
            "storage");

        job.TransitionTo(
            DocumentStatus.Queued,
            "integration-test",
            "queue");

        job.TransitionTo(
            DocumentStatus.Processing,
            "integration-test",
            "processing");

        job.TransitionTo(
            DocumentStatus.Extracted,
            "integration-test",
            "extraction");

        job.TransitionTo(
            DocumentStatus.Validating,
            "integration-test",
            "validation");

        job.TransitionTo(
            DocumentStatus.NeedsReview,
            "integration-test",
            "validation",
            "Confidence policy requires human review.");

        dbContext.DocumentJobs.Add(job);

        dbContext.DocumentAnalysisRecords.Add(
            new DocumentAnalysisRecord(
                documentId,
                "prebuilt-invoice",
                null,
                machineResultJson,
                DateTime.UtcNow));

        await dbContext.SaveChangesAsync();

        return documentId;
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
    }

    private static void AssertJsonEquivalent(
        string expected,
        string? actual)
    {
        Assert.NotNull(actual);

        using var expectedJson =
            System.Text.Json.JsonDocument.Parse(expected);

        using var actualJson =
            System.Text.Json.JsonDocument.Parse(actual);

        Assert.True(
            System.Text.Json.JsonElement.DeepEquals(
                expectedJson.RootElement,
                actualJson.RootElement),
            $"JSON documents differ.{Environment.NewLine}" +
            $"Expected: {expected}{Environment.NewLine}" +
            $"Actual: {actual}");
    }
    private static System.Text.Json.JsonSerializerOptions
        JsonOptions()
    {
        var options =
            new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        options.Converters.Add(
            new System.Text.Json.Serialization
                .JsonStringEnumConverter());

        return options;
    }
}
