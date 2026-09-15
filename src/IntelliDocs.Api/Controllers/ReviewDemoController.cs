using IntelliDocs.Core.Documents;
using IntelliDocs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntelliDocs.Api.Controllers;

[ApiController]
[Route("api/v1/demo/reviews")]
public sealed class ReviewDemoController : ControllerBase
{
    private const string DemoTenant = "demo-germany";
    private const string DemoFileName =
        "invoice-intellidocs-review-demo.pdf";

    private readonly IntelliDocsDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public ReviewDemoController(
        IntelliDocsDbContext dbContext,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [HttpPost("seed")]
    public async Task<ActionResult> Seed(
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var existingDocumentId =
            await _dbContext.DocumentJobs
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == DemoTenant &&
                    x.OriginalFileName == DemoFileName &&
                    x.ProcessingStatus ==
                    DocumentStatus.NeedsReview)
                .Select(x => (Guid?)x.DocumentId)
                .FirstOrDefaultAsync(cancellationToken);

        if (existingDocumentId.HasValue)
        {
            return Ok(
                new
                {
                    documentId = existingDocumentId.Value,
                    status = DocumentStatus.NeedsReview,
                    seeded = false
                });
        }

        var now = DateTime.UtcNow;

        var job =
            DocumentJob.Create(
                DemoTenant,
                DemoFileName,
                "9f3f44b02ecdf7371a72b74aaab62e7b" +
                "436cf75a7fbb3c8f8f82d116b02f20d6",
                now);

        job.SetStorageUri(
            $"demo://documents/{job.DocumentId}");

        job.SetDocumentType("invoice");

        job.SetAiModel(
            "prebuilt-invoice",
            "p8-demo");

        Transition(
            job,
            DocumentStatus.Stored,
            "ingestion",
            "blob-ingestion",
            "Demo document stored.");

        Transition(
            job,
            DocumentStatus.Queued,
            "ingestion",
            "service-bus",
            "Demo processing work queued.");

        Transition(
            job,
            DocumentStatus.Processing,
            "worker",
            "document-processing",
            "Demo processing started.");

        Transition(
            job,
            DocumentStatus.Extracted,
            "document-intelligence",
            "document-extraction",
            "Demo invoice extraction completed.");

        Transition(
            job,
            DocumentStatus.Validating,
            "worker",
            "business-validation",
            "Demo validation started.");

        Transition(
            job,
            DocumentStatus.NeedsReview,
            "confidence-policy",
            "business-validation",
            "Invoice total confidence is below the " +
            "automatic approval threshold.");

        const string machineResultJson =
            """
            {
              "classification": {
                "documentType": "invoice",
                "confidence": 0.97
              },
              "fields": {
                "invoiceNumber": {
                  "value": "RE-2026-0042",
                  "confidence": 0.98
                },
                "invoiceDate": {
                  "value": "2026-09-12",
                  "confidence": 0.96
                },
                "supplierName": {
                  "value": "Muster Maschinenbau GmbH",
                  "confidence": 0.95
                },
                "customerName": {
                  "value": "Contoso Deutschland GmbH",
                  "confidence": 0.94
                },
                "currency": {
                  "value": "EUR",
                  "confidence": 0.99
                },
                "totalAmount": {
                  "value": "12480.00",
                  "confidence": 0.78
                }
              },
              "validation": {
                "routingDecision": "NeedsReview",
                "issues": [
                  {
                    "code": "REVIEW_CONFIDENCE",
                    "field": "totalAmount",
                    "message": "Field confidence requires human review."
                  }
                ]
              }
            }
            """;

        var analysis =
            new DocumentAnalysisRecord(
                job.DocumentId,
                "prebuilt-invoice",
                "p8-demo",
                machineResultJson,
                now);

        _dbContext.DocumentJobs.Add(job);
        _dbContext.DocumentAnalysisRecords.Add(analysis);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Created(
            $"/api/v1/reviews/{job.DocumentId}",
            new
            {
                documentId = job.DocumentId,
                status = job.ProcessingStatus,
                seeded = true
            });
    }

    private static void Transition(
        DocumentJob job,
        DocumentStatus status,
        string actor,
        string stage,
        string reason)
    {
        job.TransitionTo(
            status,
            actor,
            stage,
            reason);
    }
}