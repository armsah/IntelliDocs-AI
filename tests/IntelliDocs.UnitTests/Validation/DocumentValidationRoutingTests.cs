using IntelliDocs.Core.Documents;

namespace IntelliDocs.UnitTests.Validation;

public sealed class DocumentValidationRoutingTests
{
    [Theory]
    [InlineData(DocumentStatus.Approved)]
    [InlineData(DocumentStatus.NeedsReview)]
    public void ValidationDecisionRoutesThroughExpectedStates(
        DocumentStatus finalStatus)
    {
        var job = CreateProcessingJob();

        job.TransitionTo(
            DocumentStatus.Extracted,
            "service-bus-worker",
            "document-processing");

        job.TransitionTo(
            DocumentStatus.Validating,
            "service-bus-worker",
            "validation");

        job.TransitionTo(
            finalStatus,
            "service-bus-worker",
            "validation");

        Assert.Equal(
            finalStatus,
            job.ProcessingStatus);

        var transitions =
            job.Transitions
                .TakeLast(3)
                .ToArray();

        Assert.Equal(
            DocumentStatus.Extracted,
            transitions[0].NextStatus);

        Assert.Equal(
            DocumentStatus.Validating,
            transitions[1].NextStatus);

        Assert.Equal(
            finalStatus,
            transitions[2].NextStatus);
    }

    [Fact]
    public void ProcessingFailureCanStillDeadLetter()
    {
        var job = CreateProcessingJob();

        Assert.True(
            job.CanTransitionTo(
                DocumentStatus.DeadLettered));

        job.TransitionTo(
            DocumentStatus.DeadLettered,
            "service-bus-worker",
            "document-processing",
            "Maximum delivery count reached.");

        Assert.Equal(
            DocumentStatus.DeadLettered,
            job.ProcessingStatus);
    }

    private static DocumentJob CreateProcessingJob()
    {
        var job =
            DocumentJob.Create(
                "tenant-test",
                "document.pdf",
                new string('a', 64));

        job.TransitionTo(
            DocumentStatus.Stored,
            "test",
            "ingestion");

        job.TransitionTo(
            DocumentStatus.Queued,
            "test",
            "queue");

        job.TransitionTo(
            DocumentStatus.Processing,
            "service-bus-worker",
            "document-processing");

        return job;
    }
}
