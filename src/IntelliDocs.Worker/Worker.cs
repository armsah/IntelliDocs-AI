using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using IntelliDocs.Core.DocumentClassification;
using IntelliDocs.Core.DocumentIntelligence;
using IntelliDocs.Core.Documents;
using IntelliDocs.Core.Messaging;
using IntelliDocs.Core.Storage;
using IntelliDocs.Core.Validation;
using IntelliDocs.Infrastructure.Observability;
using IntelliDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IntelliDocs.Worker;

public sealed class Worker : BackgroundService
{
    private const int MaxDeliveryCount = 5;

    private readonly ServiceBusProcessor _processor;
    private readonly IDbContextFactory<IntelliDocsDbContext>
        _dbContextFactory;
    private readonly IDocumentStorage _documentStorage;
    private readonly IDocumentClassifier _documentClassifier;
    private readonly IDocumentIntelligenceProvider
        _documentIntelligenceProvider;
    private readonly WorkerOptions _workerOptions;
    private readonly ILogger<Worker> _logger;

    private readonly JsonSerializerOptions _jsonOptions =
        new(JsonSerializerDefaults.Web);

    public Worker(
        ServiceBusProcessor processor,
        IDbContextFactory<IntelliDocsDbContext> dbContextFactory,
        IDocumentStorage documentStorage,
        IDocumentClassifier documentClassifier,
        IDocumentIntelligenceProvider documentIntelligenceProvider,
        IOptions<WorkerOptions> workerOptions,
        ILogger<Worker> logger)
    {
        _processor = processor;
        _dbContextFactory = dbContextFactory;
        _documentStorage = documentStorage;
        _documentClassifier = documentClassifier;
        _documentIntelligenceProvider =
            documentIntelligenceProvider;
        _workerOptions = workerOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync +=
            ProcessMessageAsync;

        _processor.ProcessErrorAsync +=
            ProcessErrorAsync;

        _logger.LogInformation(
            "Starting IntelliDocs Service Bus worker.");

        await _processor.StartProcessingAsync(
            stoppingToken);

        try
        {
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping IntelliDocs Service Bus worker.");

        await _processor.StopProcessingAsync(
            cancellationToken);

        await base.StopAsync(
            cancellationToken);
    }

    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args)
    {
        var processingStarted =
            Stopwatch.GetTimestamp();

        var message =
            await DeserializeMessageAsync(args);

        if (message is null)
        {
            return;
        }

        _logger.LogInformation(
            "Received document {DocumentId}, tenant {TenantId}, stage {ProcessingStage}, delivery {DeliveryCount}.",
            message.DocumentId,
            message.TenantId,
            message.ProcessingStage,
            args.Message.DeliveryCount);

        await using var dbContext =
            await _dbContextFactory
                .CreateDbContextAsync();

        var job =
            await dbContext.DocumentJobs
                .SingleOrDefaultAsync(
                    x =>
                        x.DocumentId ==
                        message.DocumentId);

        if (job is null)
        {
            await DeadLetterAsync(
                args,
                "DocumentNotFound",
                $"Document {message.DocumentId} does not exist.");

            return;
        }

        if (!string.Equals(
                job.TenantId,
                message.TenantId,
                StringComparison.Ordinal))
        {
            await DeadLetterAsync(
                args,
                "TenantMismatch",
                "Message tenant does not match the persisted document tenant.");

            return;
        }

        if (IsAlreadyProcessed(
                job.ProcessingStatus))
        {
            _logger.LogInformation(
                "Document {DocumentId} is already in state {Status}; completing duplicate delivery {MessageId}.",
                job.DocumentId,
                job.ProcessingStatus,
                args.Message.MessageId);

            await args.CompleteMessageAsync(
                args.Message);

            return;
        }

        if (job.ProcessingStatus ==
                DocumentStatus.DeadLettered ||
            job.ProcessingStatus ==
                DocumentStatus.Failed)
        {
            _logger.LogInformation(
                "Document {DocumentId} is in state {Status}; completing stale active-queue delivery {MessageId}.",
                job.DocumentId,
                job.ProcessingStatus,
                args.Message.MessageId);

            await args.CompleteMessageAsync(
                args.Message);

            return;
        }

        if (job.ProcessingStatus !=
                DocumentStatus.Queued &&
            job.ProcessingStatus !=
                DocumentStatus.Processing)
        {
            await DeadLetterAsync(
                args,
                "InvalidDocumentState",
                $"Document is in state {job.ProcessingStatus}; expected Queued or Processing.");

            return;
        }

        try
        {
            if (job.ProcessingStatus ==
                DocumentStatus.Queued)
            {
                job.TransitionTo(
                    DocumentStatus.Processing,
                    "service-bus-worker",
                    message.ProcessingStage,
                    $"Service Bus delivery {args.Message.DeliveryCount} started processing.");

                await dbContext
                    .SaveChangesAsync();
            }

            if (_workerOptions
                    .FailureInjectionDocumentId ==
                job.DocumentId)
            {
                throw new InvalidOperationException(
                    $"P5 deterministic failure injection for document {job.DocumentId}.");
            }

            if (string.IsNullOrWhiteSpace(
                    job.OriginalStorageUri))
            {
                throw new InvalidOperationException(
                    "Document does not have a persisted Blob Storage URI.");
            }

            var storedDocument =
                await _documentStorage
                    .OpenReadAsync(
                        job.OriginalStorageUri);

            await using var storedContent =
                storedDocument.Content;

            using var buffer =
                new MemoryStream();

            await storedContent.CopyToAsync(
                buffer);

            var content =
                buffer.ToArray();

            if (content.Length == 0)
            {
                throw new InvalidOperationException(
                    "Stored document content is empty.");
            }

            var classification =
                await _documentClassifier
                    .ClassifyAsync(
                        new DocumentClassificationRequest(
                            job.OriginalFileName,
                            storedDocument.ContentType,
                            content),
                        args.CancellationToken);

            IntelliDocsTelemetry.ClassificationConfidence.Record(
                classification.Confidence,
                IntelliDocsTelemetry.Tag(
                    "document.type",
                    classification.DocumentType.ToString()));

            var route =
                DocumentAnalysisRouter.Resolve(
                    classification.DocumentType);

            _logger.LogInformation(
                "Classified document {DocumentId} as {DocumentType} with confidence {Confidence:F4}; analysis model {AnalysisModel}.",
                job.DocumentId,
                classification.DocumentType,
                classification.Confidence,
                route.Model);

            job.SetDocumentType(
                classification.DocumentType);

            var analysis =
                await _documentIntelligenceProvider
                    .AnalyzeAsync(
                        new DocumentAnalysisRequest(
                            job.OriginalFileName,
                            storedDocument.ContentType,
                            content,
                            route.Model,
                            route.QueryFields),
                        args.CancellationToken);

            IntelliDocsTelemetry.DocumentIntelligenceOperations.Add(
                1,
                IntelliDocsTelemetry.Tag(
                    "operation",
                    "analyze"),
                IntelliDocsTelemetry.Tag(
                    "document.type",
                    classification.DocumentType.ToString()));

            if (job.ProcessingStatus ==
                DocumentStatus.Processing)
            {
                job.TransitionTo(
                    DocumentStatus.Extracted,
                    "service-bus-worker",
                    message.ProcessingStage,
                    "Document classification and Document Intelligence extraction completed.");

                job.TransitionTo(
                    DocumentStatus.Validating,
                    "service-bus-worker",
                    "validation",
                    "Deterministic confidence policy and business validation started.");
            }

            var validation =
                DocumentConfidencePolicy.Evaluate(
                    classification,
                    analysis);

            IntelliDocsTelemetry.PolicyConfidence.Record(
                validation.PolicyConfidence,
                IntelliDocsTelemetry.Tag(
                    "document.type",
                    classification.DocumentType.ToString()));

            IntelliDocsTelemetry.RoutingDecisions.Add(
                1,
                IntelliDocsTelemetry.Tag(
                    "decision",
                    validation.Decision.ToString()),
                IntelliDocsTelemetry.Tag(
                    "document.type",
                    classification.DocumentType.ToString()));

            foreach (var issue in validation.Issues)
            {
                IntelliDocsTelemetry.ValidationIssues.Add(
                    1,
                    IntelliDocsTelemetry.Tag(
                        "severity",
                        issue.Severity.ToString()),
                    IntelliDocsTelemetry.Tag(
                        "document.type",
                        classification.DocumentType.ToString()));
            }

            var processingResult =
                new DocumentProcessingResult(
                    classification,
                    analysis,
                    validation);

            var resultJson =
                JsonSerializer.Serialize(
                    processingResult,
                    _jsonOptions);

            var existingAnalysis =
                await dbContext
                    .DocumentAnalysisRecords
                    .SingleOrDefaultAsync(
                        x =>
                            x.DocumentId ==
                            job.DocumentId);

            if (existingAnalysis is null)
            {
                dbContext
                    .DocumentAnalysisRecords
                    .Add(
                        new DocumentAnalysisRecord(
                            job.DocumentId,
                            analysis.ModelId,
                            analysis.ModelVersion,
                            resultJson,
                            DateTime.UtcNow));
            }

            job.SetAiModel(
                analysis.ModelId,
                analysis.ModelVersion);

            var finalStatus =
                validation.Decision ==
                    DocumentRoutingDecision.Approved
                    ? DocumentStatus.Approved
                    : DocumentStatus.NeedsReview;

            if (job.ProcessingStatus ==
                DocumentStatus.Validating)
            {
                job.TransitionTo(
                    finalStatus,
                    "service-bus-worker",
                    "validation",
                    $"Policy confidence {validation.PolicyConfidence:F4}; decision {validation.Decision}; issues {validation.Issues.Count}.");
            }

            _logger.LogInformation(
                "Validated document {DocumentId}; policy confidence {PolicyConfidence:F4}; decision {Decision}; issues {IssueCount}.",
                job.DocumentId,
                validation.PolicyConfidence,
                validation.Decision,
                validation.Issues.Count);

            await dbContext
                .SaveChangesAsync();

            await args.CompleteMessageAsync(
                args.Message);

            var processingDuration =
                Stopwatch.GetElapsedTime(
                    processingStarted);

            IntelliDocsTelemetry.DocumentsProcessed.Add(
                1,
                IntelliDocsTelemetry.Tag(
                    "outcome",
                    "success"),
                IntelliDocsTelemetry.Tag(
                    "document.type",
                    classification.DocumentType.ToString()));

            IntelliDocsTelemetry.ProcessingDuration.Record(
                processingDuration.TotalSeconds,
                IntelliDocsTelemetry.Tag(
                    "outcome",
                    "success"),
                IntelliDocsTelemetry.Tag(
                    "document.type",
                    classification.DocumentType.ToString()));

            _logger.LogInformation(
                "Completed document {DocumentId}; classified as {DocumentType} with confidence {Confidence:F4}; Service Bus message {MessageId} settled successfully.",
                job.DocumentId,
                classification.DocumentType,
                classification.Confidence,
                args.Message.MessageId);
        }
        catch (Exception exception)
            when (!args.CancellationToken
                .IsCancellationRequested)
        {
            await HandleProcessingFailureAsync(
                args,
                job,
                dbContext,
                message,
                exception);
        }
    }

    private async Task<DocumentProcessingMessage?>
        DeserializeMessageAsync(
            ProcessMessageEventArgs args)
    {
        DocumentProcessingMessage? message;

        try
        {
            message =
                args.Message.Body
                    .ToObjectFromJson<
                        DocumentProcessingMessage>(
                        _jsonOptions);
        }
        catch (Exception exception)
            when (exception is JsonException ||
                  exception is NotSupportedException)
        {
            _logger.LogWarning(
                exception,
                "Dead-lettering malformed Service Bus message {MessageId}.",
                args.Message.MessageId);

            await DeadLetterAsync(
                args,
                "InvalidMessage",
                "Message body could not be deserialized as DocumentProcessingMessage.");

            return null;
        }

        if (message is null ||
            message.DocumentId == Guid.Empty ||
            string.IsNullOrWhiteSpace(
                message.TenantId) ||
            string.IsNullOrWhiteSpace(
                message.ProcessingStage))
        {
            await DeadLetterAsync(
                args,
                "InvalidMessage",
                "Required document-processing message fields are missing.");

            return null;
        }

        return message;
    }

    private async Task HandleProcessingFailureAsync(
        ProcessMessageEventArgs args,
        DocumentJob job,
        IntelliDocsDbContext dbContext,
        DocumentProcessingMessage message,
        Exception exception)
    {
        _logger.LogWarning(
            exception,
            "Processing failed for document {DocumentId}, delivery {DeliveryCount} of {MaxDeliveryCount}.",
            job.DocumentId,
            args.Message.DeliveryCount,
            MaxDeliveryCount);

        await dbContext
            .Entry(job)
            .ReloadAsync(
                args.CancellationToken);

        if (args.Message.DeliveryCount >=
            MaxDeliveryCount)
        {
            if (job.ProcessingStatus ==
                DocumentStatus.Processing)
            {
                job.TransitionTo(
                    DocumentStatus.DeadLettered,
                    "service-bus-worker",
                    message.ProcessingStage,
                    $"Processing failed after {args.Message.DeliveryCount} deliveries: {exception.Message}");

                await dbContext
                    .SaveChangesAsync();
            }

            await DeadLetterAsync(
                args,
                "ProcessingFailed",
                $"Document processing failed after {args.Message.DeliveryCount} deliveries. {exception.Message}");

            IntelliDocsTelemetry.DeadLetters.Add(
                1,
                IntelliDocsTelemetry.Tag(
                    "outcome",
                    "processing_failed"));

            IntelliDocsTelemetry.DocumentsProcessed.Add(
                1,
                IntelliDocsTelemetry.Tag(
                    "outcome",
                    "dead_letter"));

            _logger.LogError(
                exception,
                "Document {DocumentId} was dead-lettered after {DeliveryCount} deliveries.",
                job.DocumentId,
                args.Message.DeliveryCount);

            return;
        }

        IntelliDocsTelemetry.ProcessingRetries.Add(
            1,
            IntelliDocsTelemetry.Tag(
                "outcome",
                "retry"));

        await args.AbandonMessageAsync(
            args.Message);

        _logger.LogInformation(
            "Abandoned document {DocumentId} message {MessageId}; Service Bus will redeliver it.",
            job.DocumentId,
            args.Message.MessageId);
    }

    private async Task DeadLetterAsync(
        ProcessMessageEventArgs args,
        string reason,
        string description)
    {
        var safeDescription =
            description.Length <= 1024
                ? description
                : description[..1024];

        _logger.LogWarning(
            "Dead-lettering message {MessageId}. Reason: {Reason}. Description: {Description}",
            args.Message.MessageId,
            reason,
            safeDescription);

        await args.DeadLetterMessageAsync(
            args.Message,
            reason,
            safeDescription);
    }

    private static bool IsAlreadyProcessed(
        DocumentStatus status)
    {
        return status is
            DocumentStatus.Extracted or
            DocumentStatus.Validating or
            DocumentStatus.Approved or
            DocumentStatus.NeedsReview or
            DocumentStatus.InReview or
            DocumentStatus.Rejected or
            DocumentStatus.Publishing or
            DocumentStatus.Completed;
    }

    private Task ProcessErrorAsync(
        ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processor error. Entity: {EntityPath}; Source: {ErrorSource}.",
            args.EntityPath,
            args.ErrorSource);

        return Task.CompletedTask;
    }
}
