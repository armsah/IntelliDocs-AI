using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace IntelliDocs.Infrastructure.Observability;

public static class IntelliDocsTelemetry
{
    public const string SourceName = "IntelliDocs";
    public const string MeterName = "IntelliDocs";

    public static readonly ActivitySource ActivitySource =
        new(SourceName);

    private static readonly Meter Meter =
        new(MeterName);

    public static readonly Counter<long> DocumentsProcessed =
        Meter.CreateCounter<long>(
            "intellidocs.documents.processed",
            "{document}",
            "Documents reaching a processing outcome.");

    public static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>(
            "intellidocs.document.processing.duration",
            "s",
            "End-to-end document processing duration.");

    public static readonly Histogram<double> ClassificationConfidence =
        Meter.CreateHistogram<double>(
            "intellidocs.ai.classification.confidence",
            "{score}",
            "Document classification confidence.");

    public static readonly Histogram<double> PolicyConfidence =
        Meter.CreateHistogram<double>(
            "intellidocs.ai.policy.confidence",
            "{score}",
            "Confidence used by the deterministic routing policy.");

    public static readonly Counter<long> RoutingDecisions =
        Meter.CreateCounter<long>(
            "intellidocs.routing.decisions",
            "{decision}",
            "Deterministic document routing decisions.");

    public static readonly Counter<long> ValidationIssues =
        Meter.CreateCounter<long>(
            "intellidocs.validation.issues",
            "{issue}",
            "Business validation issues.");

    public static readonly Counter<long> ReviewCorrections =
        Meter.CreateCounter<long>(
            "intellidocs.review.corrections",
            "{correction}",
            "Human-review corrections.");

    public static readonly Counter<long> ReviewDecisions =
        Meter.CreateCounter<long>(
            "intellidocs.review.decisions",
            "{decision}",
            "Completed human-review decisions.");

    public static readonly Counter<long> ProcessingRetries =
        Meter.CreateCounter<long>(
            "intellidocs.processing.retries",
            "{retry}",
            "Document-processing retries.");

    public static readonly Counter<long> DeadLetters =
        Meter.CreateCounter<long>(
            "intellidocs.processing.deadletters",
            "{document}",
            "Documents moved to the dead-letter path.");

    public static readonly Counter<long> DocumentIntelligenceOperations =
        Meter.CreateCounter<long>(
            "intellidocs.ai.document_intelligence.operations",
            "{operation}",
            "Document Intelligence operations used as a cost/usage signal.");

    public static KeyValuePair<string, object?> Tag(
        string name,
        object? value) =>
        new(name, value);
}