using System.Security.Cryptography;
using IntelliDocs.Api.Models;
using IntelliDocs.Core.Documents;
using IntelliDocs.Core.Storage;
using IntelliDocs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntelliDocs.Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IntelliDocsDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IDocumentStorage _documentStorage;

    public DocumentsController(
        IntelliDocsDbContext dbContext,
        IWebHostEnvironment environment,
        IDocumentStorage documentStorage)
    {
        _dbContext = dbContext;
        _environment = environment;
        _documentStorage = documentStorage;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<DocumentJobResponse>> Upload(
        [FromForm] string tenantId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return BadRequest(new
            {
                error = "tenantId is required."
            });
        }

        if (file.Length == 0)
        {
            return BadRequest(new
            {
                error = "A non-empty document is required."
            });
        }

        var safeFileName = Path.GetFileName(file.FileName);

        await using var inputStream = file.OpenReadStream();

        var hashBytes =
            await SHA256.HashDataAsync(
                inputStream,
                cancellationToken);

        var sha256 =
            Convert.ToHexString(hashBytes)
                   .ToLowerInvariant();

        var existingDocument =
            await _dbContext.DocumentJobs
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.Sha256 == sha256,
                    cancellationToken);

        if (existingDocument is not null)
        {
            return Conflict(new
            {
                error = "Duplicate document content already exists for this tenant.",
                documentId = existingDocument.DocumentId,
                sha256 = existingDocument.Sha256
            });
        }

        var job = DocumentJob.Create(
            tenantId,
            safeFileName,
            sha256);

        _dbContext.DocumentJobs.Add(job);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();

            var concurrentDuplicate =
                await _dbContext.DocumentJobs
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.Sha256 == sha256,
                        cancellationToken);

            if (concurrentDuplicate is null)
            {
                throw;
            }

            return Conflict(new
            {
                error = "Duplicate document content already exists for this tenant.",
                documentId = concurrentDuplicate.DocumentId,
                sha256 = concurrentDuplicate.Sha256
            });
        }

        await using var documentStream =
            file.OpenReadStream();

        var storageResult =
            await _documentStorage.StoreAsync(
                new DocumentStorageRequest(
                    job.DocumentId,
                    tenantId,
                    safeFileName,
                    file.ContentType,
                    sha256,
                    documentStream),
                cancellationToken);

        job.SetStorageUri(storageResult.StorageUri);

        job.TransitionTo(
            DocumentStatus.Stored,
            "local-api",
            "storage",
            "Document persisted to Azure Blob Storage.");

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                documentId = job.DocumentId
            },
            ToResponse(job));
    }

    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<DocumentJobResponse>> GetById(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var job =
            await _dbContext.DocumentJobs
                .Include(x => x.Transitions)
                .SingleOrDefaultAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (job is null)
            return NotFound();

        return Ok(ToResponse(job));
    }

    [HttpPost("{documentId:guid}/transitions")]
    public async Task<ActionResult<DocumentJobResponse>> Transition(
        Guid documentId,
        TransitionDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var job =
            await _dbContext.DocumentJobs
                .Include(x => x.Transitions)
                .SingleOrDefaultAsync(
                    x => x.DocumentId == documentId,
                    cancellationToken);

        if (job is null)
            return NotFound();

        try
        {
            job.TransitionTo(
                request.NextStatus,
                request.Actor,
                request.ProcessingStage,
                request.Reason);
        }
        catch (InvalidDocumentStateTransitionException exception)
        {
            return Conflict(new
            {
                error = exception.Message,
                currentStatus = exception.CurrentStatus,
                requestedStatus = exception.RequestedStatus
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Ok(ToResponse(job));
    }

    private static DocumentJobResponse ToResponse(
        DocumentJob job)
    {
        return new DocumentJobResponse(
            job.DocumentId,
            job.TenantId,
            job.OriginalFileName,
            job.OriginalStorageUri,
            job.Sha256,
            job.DetectedType,
            job.AiModelId,
            job.AiModelVersion,
            job.ProcessingStatus,
            job.SubmittedAtUtc,
            job.CompletedAtUtc,
            job.Transitions
                .OrderBy(x => x.OccurredAtUtc)
                .Select(x =>
                    new DocumentTransitionResponse(
                        x.Id,
                        x.PreviousStatus,
                        x.NextStatus,
                        x.OccurredAtUtc,
                        x.Actor,
                        x.ProcessingStage,
                        x.Reason))
                .ToArray());
    }
}
