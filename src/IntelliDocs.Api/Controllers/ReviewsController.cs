using IntelliDocs.Api.Models.Reviews;
using IntelliDocs.Core.Documents;
using IntelliDocs.Core.Reviews;
using IntelliDocs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using IntelliDocs.Infrastructure.Observability;

namespace IntelliDocs.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/reviews")]
public sealed class ReviewsController : ControllerBase
{
    private const string ReviewStage = "human-review";

    private readonly IntelliDocsDbContext _dbContext;

    public ReviewsController(
        IntelliDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<DocumentReviewQueueItemResponse>>> GetQueue(
        CancellationToken cancellationToken)
    {
        var documents =
            await _dbContext.DocumentJobs
                .AsNoTracking()
                .Where(x =>
                    x.ProcessingStatus ==
                    DocumentStatus.NeedsReview)
                .OrderBy(x => x.SubmittedAtUtc)
                .Select(x =>
                    new DocumentReviewQueueItemResponse(
                        x.DocumentId,
                        x.TenantId,
                        x.OriginalFileName,
                        x.DetectedType,
                        x.ProcessingStatus,
                        x.SubmittedAtUtc))
                .ToListAsync(cancellationToken);

        return Ok(documents);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<DocumentReviewResponse>> Get(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var review =
            await _dbContext.DocumentReviews
                .AsNoTracking()
                .Include(x => x.Corrections)
                .SingleOrDefaultAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (review is null)
        {
            return NotFound();
        }

        return Ok(
            await ToResponseAsync(
                review,
                cancellationToken));
    }

    [HttpPost("{documentId:guid}/start")]
    public async Task<ActionResult<DocumentReviewResponse>> Start(
        Guid documentId,
        StartDocumentReviewRequest request,
        CancellationToken cancellationToken)
    {

        var job =
            await _dbContext.DocumentJobs
                .SingleOrDefaultAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        if (job.ProcessingStatus !=
            DocumentStatus.NeedsReview)
        {
            return Conflict(
                new
                {
                    error =
                        "Document must be in NeedsReview " +
                        "before review can start.",
                    currentStatus =
                        job.ProcessingStatus
                });
        }

        var existingReview =
            await _dbContext.DocumentReviews
                .AnyAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (existingReview)
        {
            return Conflict(
                new
                {
                    error =
                        "A review already exists for this document."
                });
        }

        var reviewer = GetReviewerIdentity();

        var review =
            DocumentReview.Start(
                documentId,
                reviewer);

        job.TransitionTo(
            DocumentStatus.InReview,
            reviewer,
            ReviewStage,
            "Human review started.");

        _dbContext.DocumentReviews.Add(review);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(
                new
                {
                    error =
                        "The review could not be started because " +
                        "the document was modified concurrently."
                });
        }

        return Ok(
            await ToResponseAsync(
                review,
                cancellationToken));
    }

    [HttpPost("{documentId:guid}/corrections")]
    public async Task<ActionResult<DocumentReviewResponse>>
        AddCorrection(
            Guid documentId,
            AddDocumentReviewCorrectionRequest request,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FieldName))
        {
            return BadRequest(
                new
                {
                    error =
                        "FieldName is required."
                });
        }

        var job =
            await _dbContext.DocumentJobs
                .SingleOrDefaultAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        if (job.ProcessingStatus !=
            DocumentStatus.InReview)
        {
            return Conflict(
                new
                {
                    error =
                        "Corrections can only be added while " +
                        "the document is InReview.",
                    currentStatus =
                        job.ProcessingStatus
                });
        }

        var review =
            await _dbContext.DocumentReviews
                .Include(x => x.Corrections)
                .SingleOrDefaultAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (review is null)
        {
            return Conflict(
                new
                {
                    error =
                        "No review exists for this document."
                });
        }

        var reviewer = GetReviewerIdentity();

        try
        {
            review.AddCorrection(
                request.FieldName,
                request.OriginalValue,
                request.CorrectedValue,
                reviewer);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                new
                {
                    error = exception.Message
                });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new
                {
                    error = exception.Message
                });
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        IntelliDocsTelemetry.ReviewCorrections.Add(
            1,
            IntelliDocsTelemetry.Tag(
                "document.type",
                job.DetectedType?.ToString() ?? "unknown"));

        return Ok(
            await ToResponseAsync(
                review,
                cancellationToken));
    }

    [HttpPost("{documentId:guid}/decision")]
    public async Task<ActionResult<DocumentReviewResponse>>
        Complete(
            Guid documentId,
            CompleteDocumentReviewRequest request,
            CancellationToken cancellationToken)
    {


        if (!Enum.IsDefined(request.Decision))
        {
            return BadRequest(
                new
                {
                    error = "Review decision is invalid."
                });
        }

        var job =
            await _dbContext.DocumentJobs
                .SingleOrDefaultAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        if (job.ProcessingStatus !=
            DocumentStatus.InReview)
        {
            return Conflict(
                new
                {
                    error =
                        "A decision can only be recorded while " +
                        "the document is InReview.",
                    currentStatus =
                        job.ProcessingStatus
                });
        }

        var review =
            await _dbContext.DocumentReviews
                .Include(x => x.Corrections)
                .SingleOrDefaultAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (review is null)
        {
            return Conflict(
                new
                {
                    error =
                        "No review exists for this document."
                });
        }

        var reviewer = GetReviewerIdentity();

        try
        {
            review.Complete(
                request.Decision,
                reviewer,
                request.Reason);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                new
                {
                    error = exception.Message
                });
        }

        var nextStatus =
            request.Decision ==
            DocumentReviewDecision.Approved
                ? DocumentStatus.Approved
                : DocumentStatus.Rejected;

        job.TransitionTo(
            nextStatus,
            reviewer,
            ReviewStage,
            request.Reason);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        IntelliDocsTelemetry.ReviewDecisions.Add(
            1,
            IntelliDocsTelemetry.Tag(
                "decision",
                request.Decision.ToString()),
            IntelliDocsTelemetry.Tag(
                "document.type",
                job.DetectedType?.ToString() ?? "unknown"));

        return Ok(
            await ToResponseAsync(
                review,
                cancellationToken));
    }

    private string GetReviewerIdentity()
    {
        var reviewer =
            User.FindFirstValue("oid") ??
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(reviewer))
        {
            throw new InvalidOperationException(
                "Authenticated reviewer identity is unavailable.");
        }

        return reviewer;
    }

    private async Task<DocumentReviewResponse> ToResponseAsync(
        DocumentReview review,
        CancellationToken cancellationToken)
    {
        var job =
            await _dbContext.DocumentJobs
                .AsNoTracking()
                .SingleAsync(
                    x => x.DocumentId == review.DocumentId,
                    cancellationToken);

        var machineResultJson =
            await _dbContext.DocumentAnalysisRecords
                .AsNoTracking()
                .Where(x =>
                    x.DocumentId == review.DocumentId)
                .Select(x => x.ResultJson)
                .SingleOrDefaultAsync(cancellationToken);

        var corrections =
            review.Corrections
                .OrderBy(x => x.CorrectedAtUtc)
                .Select(x =>
                    new DocumentReviewCorrectionResponse(
                        x.Id,
                        x.FieldName,
                        x.OriginalValue,
                        x.CorrectedValue,
                        x.Reviewer,
                        x.CorrectedAtUtc))
                .ToList();

        return new DocumentReviewResponse(
            review.Id,
            review.DocumentId,
            job.ProcessingStatus,
            job.TenantId,
            job.OriginalFileName,
            job.DetectedType,
            review.Reviewer,
            review.StartedAtUtc,
            review.Decision,
            review.DecisionReason,
            review.DecidedAtUtc,
            corrections,
            machineResultJson);
    }
}
