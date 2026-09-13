using IntelliDocs.Core.Documents;

namespace IntelliDocs.UnitTests.Documents;

public sealed class DocumentJobTests
{
    private const string Sha256 =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void Create_StartsJobInSubmittedState()
    {
        var submittedAt =
            new DateTime(2026, 9, 13, 10, 0, 0, DateTimeKind.Utc);

        var job = DocumentJob.Create(
            "tenant-001",
            "invoice-001.pdf",
            Sha256,
            submittedAt);

        Assert.NotEqual(Guid.Empty, job.DocumentId);
        Assert.Equal("tenant-001", job.TenantId);
        Assert.Equal("invoice-001.pdf", job.OriginalFileName);
        Assert.Equal(Sha256, job.Sha256);
        Assert.Equal(DocumentStatus.Submitted, job.ProcessingStatus);
        Assert.Equal(submittedAt, job.SubmittedAtUtc);
        Assert.Null(job.CompletedAtUtc);
        Assert.Empty(job.Transitions);
    }

    [Fact]
    public void TransitionTo_ValidTransition_ChangesStateAndCreatesAuditRecord()
    {
        var job = CreateJob();

        var transitionAt =
            new DateTime(2026, 9, 13, 10, 1, 0, DateTimeKind.Utc);

        job.TransitionTo(
            DocumentStatus.Stored,
            "local-api",
            "storage",
            "Document persisted to local storage.",
            transitionAt);

        Assert.Equal(DocumentStatus.Stored, job.ProcessingStatus);

        var transition = Assert.Single(job.Transitions);

        Assert.Equal(DocumentStatus.Submitted, transition.PreviousStatus);
        Assert.Equal(DocumentStatus.Stored, transition.NextStatus);
        Assert.Equal("local-api", transition.Actor);
        Assert.Equal("storage", transition.ProcessingStage);
        Assert.Equal(
            "Document persisted to local storage.",
            transition.Reason);
        Assert.Equal(transitionAt, transition.OccurredAtUtc);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_Throws()
    {
        var job = CreateJob();

        var exception =
            Assert.Throws<InvalidDocumentStateTransitionException>(
                () => job.TransitionTo(
                    DocumentStatus.Completed,
                    "test",
                    "invalid-test"));

        Assert.Equal(DocumentStatus.Submitted, exception.CurrentStatus);
        Assert.Equal(DocumentStatus.Completed, exception.RequestedStatus);
        Assert.Equal(DocumentStatus.Submitted, job.ProcessingStatus);
        Assert.Empty(job.Transitions);
    }

    [Fact]
    public void AutoApprovalLifecycle_CanReachCompleted()
    {
        var job = CreateJob();

        Transition(job, DocumentStatus.Stored, "storage");
        Transition(job, DocumentStatus.Queued, "queue");
        Transition(job, DocumentStatus.Processing, "processing");
        Transition(job, DocumentStatus.Extracted, "extraction");
        Transition(job, DocumentStatus.Validating, "validation");
        Transition(job, DocumentStatus.Approved, "approval");
        Transition(job, DocumentStatus.Publishing, "publishing");
        Transition(job, DocumentStatus.Completed, "publishing");

        Assert.Equal(DocumentStatus.Completed, job.ProcessingStatus);
        Assert.NotNull(job.CompletedAtUtc);
        Assert.Equal(8, job.Transitions.Count);
        Assert.False(job.CanTransitionTo(DocumentStatus.Queued));
    }

    [Fact]
    public void HumanReviewLifecycle_CanReachApproved()
    {
        var job = CreateJob();

        Transition(job, DocumentStatus.Stored, "storage");
        Transition(job, DocumentStatus.Queued, "queue");
        Transition(job, DocumentStatus.Processing, "processing");
        Transition(job, DocumentStatus.Extracted, "extraction");
        Transition(job, DocumentStatus.Validating, "validation");
        Transition(job, DocumentStatus.NeedsReview, "validation");
        Transition(job, DocumentStatus.InReview, "human-review");
        Transition(job, DocumentStatus.Approved, "human-review");

        Assert.Equal(DocumentStatus.Approved, job.ProcessingStatus);
    }

    [Fact]
    public void FailedJob_CanBeRequeued()
    {
        var job = CreateJob();

        Transition(job, DocumentStatus.Stored, "storage");
        Transition(job, DocumentStatus.Queued, "queue");
        Transition(job, DocumentStatus.Processing, "processing");

        job.TransitionTo(
            DocumentStatus.Failed,
            "worker",
            "processing",
            "Transient processing failure.");

        Assert.True(job.CanTransitionTo(DocumentStatus.Queued));

        job.TransitionTo(
            DocumentStatus.Queued,
            "operator",
            "retry",
            "Retry requested.");

        Assert.Equal(DocumentStatus.Queued, job.ProcessingStatus);
    }

    [Fact]
    public void DeadLetteredJob_CanBeRedriven()
    {
        var job = CreateJob();

        Transition(job, DocumentStatus.Stored, "storage");
        Transition(job, DocumentStatus.Queued, "queue");
        Transition(job, DocumentStatus.Processing, "processing");
        Transition(job, DocumentStatus.DeadLettered, "processing");

        Assert.True(job.CanTransitionTo(DocumentStatus.Queued));

        Transition(job, DocumentStatus.Queued, "redrive");

        Assert.Equal(DocumentStatus.Queued, job.ProcessingStatus);
    }

    [Fact]
    public void RejectedJob_IsTerminal()
    {
        var job = CreateJob();

        Transition(job, DocumentStatus.Stored, "storage");
        Transition(job, DocumentStatus.Queued, "queue");
        Transition(job, DocumentStatus.Processing, "processing");
        Transition(job, DocumentStatus.Extracted, "extraction");
        Transition(job, DocumentStatus.Validating, "validation");
        Transition(job, DocumentStatus.NeedsReview, "validation");
        Transition(job, DocumentStatus.InReview, "human-review");
        Transition(job, DocumentStatus.Rejected, "human-review");

        Assert.Equal(DocumentStatus.Rejected, job.ProcessingStatus);
        Assert.False(job.CanTransitionTo(DocumentStatus.Queued));
        Assert.False(job.CanTransitionTo(DocumentStatus.Approved));
    }

    private static DocumentJob CreateJob()
    {
        return DocumentJob.Create(
            "tenant-001",
            "sample.pdf",
            Sha256);
    }

    private static void Transition(
        DocumentJob job,
        DocumentStatus status,
        string stage)
    {
        job.TransitionTo(
            status,
            "unit-test",
            stage);
    }
}
