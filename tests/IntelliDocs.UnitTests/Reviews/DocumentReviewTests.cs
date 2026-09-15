using IntelliDocs.Core.Reviews;

namespace IntelliDocs.UnitTests.Reviews;

public sealed class DocumentReviewTests
{
    [Fact]
    public void Start_CreatesOpenReviewForAssignedReviewer()
    {
        var documentId = Guid.NewGuid();
        var startedAt = new DateTime(
            2026, 9, 15, 10, 0, 0,
            DateTimeKind.Utc);

        var review = DocumentReview.Start(
            documentId,
            "reviewer@example.com",
            startedAt);

        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal(documentId, review.DocumentId);
        Assert.Equal("reviewer@example.com", review.Reviewer);
        Assert.Equal(startedAt, review.StartedAtUtc);
        Assert.False(review.IsCompleted);
        Assert.Null(review.Decision);
        Assert.Null(review.DecisionReason);
        Assert.Null(review.DecidedAtUtc);
        Assert.Empty(review.Corrections);
    }

    [Fact]
    public void AddCorrection_AppendsAuditableCorrection()
    {
        var review = DocumentReview.Start(
            Guid.NewGuid(),
            "reviewer@example.com");

        var correctedAt = new DateTime(
            2026, 9, 15, 10, 5, 0,
            DateTimeKind.Utc);

        review.AddCorrection(
            "totalAmount",
            "1250.00",
            "1520.00",
            "reviewer@example.com",
            correctedAt);

        var correction = Assert.Single(review.Corrections);

        Assert.NotEqual(Guid.Empty, correction.Id);
        Assert.Equal(review.Id, correction.ReviewId);
        Assert.Equal("totalAmount", correction.FieldName);
        Assert.Equal("1250.00", correction.OriginalValue);
        Assert.Equal("1520.00", correction.CorrectedValue);
        Assert.Equal("reviewer@example.com", correction.Reviewer);
        Assert.Equal(correctedAt, correction.CorrectedAtUtc);
    }

    [Fact]
    public void Complete_Approved_RecordsDecisionAudit()
    {
        var review = DocumentReview.Start(
            Guid.NewGuid(),
            "reviewer@example.com");

        var decidedAt = new DateTime(
            2026, 9, 15, 10, 10, 0,
            DateTimeKind.Utc);

        review.Complete(
            DocumentReviewDecision.Approved,
            "reviewer@example.com",
            "Corrected total verified.",
            decidedAt);

        Assert.True(review.IsCompleted);
        Assert.Equal(
            DocumentReviewDecision.Approved,
            review.Decision);
        Assert.Equal(
            "Corrected total verified.",
            review.DecisionReason);
        Assert.Equal(decidedAt, review.DecidedAtUtc);
    }

    [Fact]
    public void CompletedReview_CannotBeModified()
    {
        var review = DocumentReview.Start(
            Guid.NewGuid(),
            "reviewer@example.com");

        review.Complete(
            DocumentReviewDecision.Rejected,
            "reviewer@example.com",
            "Document is invalid.");

        Assert.Throws<InvalidOperationException>(
            () => review.AddCorrection(
                "invoiceNumber",
                "INV-1",
                "INV-2",
                "reviewer@example.com"));
    }

    [Fact]
    public void DifferentReviewer_CannotAddCorrection()
    {
        var review = DocumentReview.Start(
            Guid.NewGuid(),
            "assigned@example.com");

        Assert.Throws<InvalidOperationException>(
            () => review.AddCorrection(
                "invoiceNumber",
                "INV-1",
                "INV-2",
                "other@example.com"));
    }

    [Fact]
    public void DifferentReviewer_CannotCompleteReview()
    {
        var review = DocumentReview.Start(
            Guid.NewGuid(),
            "assigned@example.com");

        Assert.Throws<InvalidOperationException>(
            () => review.Complete(
                DocumentReviewDecision.Approved,
                "other@example.com"));
    }
}
