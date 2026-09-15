# P8 — Human Review Portal Evidence

## Objective

Build an auditable human-review workflow for documents routed to manual review.

P8 exit criterion:

> Corrections auditable.

The implemented workflow is:

`NeedsReview -> InReview -> Approved / Rejected`

PostgreSQL remains the source of truth for document processing state and review audit data.

## Review architecture

P8 adds a Blazor Server human-review portal backed by ASP.NET Core review APIs.

The review workflow separates machine-produced extraction data from human corrections:

- the original machine analysis remains stored in `DocumentAnalysisRecord.ResultJson`;
- starting a review creates a `DocumentReview`;
- each field correction creates a separate `DocumentReviewCorrection`;
- reviewer identity and correction timestamp are persisted;
- the final approval or rejection decision, reason, reviewer, and timestamp are persisted;
- document state transitions continue through the existing `DocumentJob` aggregate and transition audit trail.

The original machine extraction JSON is not overwritten by human corrections.

## Review API

The API exposes the following P8 workflow endpoints:

- `GET /api/v1/reviews` — list documents currently requiring review;
- `GET /api/v1/reviews/{documentId}` — load an existing review and immutable machine result;
- `POST /api/v1/reviews/{documentId}/start` — claim a `NeedsReview` document and transition it to `InReview`;
- `POST /api/v1/reviews/{documentId}/corrections` — append an auditable field correction;
- `POST /api/v1/reviews/{documentId}/decision` — approve or reject the review.

Review operations enforce the assigned reviewer and valid processing state.

## Human-review portal

`IntelliDocs.ReviewPortal` is a Blazor Server application.

The queue page provides:

- documents in `NeedsReview`;
- explicit reviewer identity input;
- review claiming;
- navigation to the review workspace.

The review workspace provides:

- document metadata;
- immutable machine-result JSON;
- existing correction audit records;
- field correction entry;
- approval or rejection with a decision reason;
- completed-review state.

Explicit reviewer entry is a P8 boundary. P9 replaces this with Microsoft Entra authenticated identity.

## Browser review demo

A Development-only seed endpoint created a reproducible invoice requiring human review.

Demo document:

- tenant: `demo-germany`;
- file: `invoice-intellidocs-review-demo.pdf`;
- detected type: `invoice`;
- initial processing status: `NeedsReview`;
- machine `totalAmount`: `12480.00`;
- machine confidence for `totalAmount`: `0.78`.

The browser workflow was executed with reviewer:

`reviewer@intellidocs.local`

The reviewer claimed the document, producing the transition:

`NeedsReview -> InReview`

The following correction was submitted:

- field: `totalAmount`;
- original value: `12480.00`;
- corrected value: `12408.00`.

The review was then approved with reason:

`Verified invoice total against source document.`

The resulting document processing status was:

`Approved`

## Audit verification

API verification after the browser workflow returned:

- review ID: `38003f71-c517-4d8c-8495-8378b2e2a55a`;
- document ID: `47474c95-1680-4b08-a654-b4d17263ac3d`;
- processing status: `Approved`;
- reviewer: `reviewer@intellidocs.local`;
- decision: `Approved`;
- correction ID: `8a55ff82-c22e-42e9-b687-f5dd40194468`;
- corrected `totalAmount`: `12408.00`.

The returned `machineResultJson` still contained the original machine value:

`12480.00`

with confidence:

`0.78`

This verifies that human correction data is auditable without mutating the original machine result.

After approval, the document no longer appeared in the `NeedsReview` queue.

The Development-only seed endpoint makes the demo reproducible. Later integration-test cleanup removed the original demo database record, so the identifiers above are evidence from the observed browser/API run rather than permanent fixture identifiers.

## Persistence

P8 adds PostgreSQL persistence for:

- `document_reviews`;
- `document_review_corrections`.

The existing `document_job_transitions` audit trail records the human-review state changes.

The migration is:

`20260915114911_AddDocumentReviewAudit`

No machine-analysis table or JSON payload is replaced by a correction.

## Automated test evidence

Full solution regression:

- total: 63;
- passed: 63;
- failed: 0;
- skipped: 0.

P8 review-domain unit tests:

- total: 6;
- passed: 6;
- failed: 0.

P8 review-workflow integration tests:

- total: 2;
- passed: 2;
- failed: 0.

The integration tests verify both:

- correction followed by approval;
- rejection.

Database logs during the integration tests show writes to review, correction, document-state, and transition persistence.

After the Linux build portability repair to currency normalization, the P7 validation suite was also rerun:

- total: 29;
- passed: 29;
- failed: 0.

## Container evidence

P8 adds Linux container build definitions for:

- `IntelliDocs.Api`;
- `IntelliDocs.ReviewPortal`.

Both images were successfully built with Docker Desktop using the .NET 10 Linux SDK/runtime images.

Verified images:

- `intellidocs-api:p8` — Linux/amd64;
- `intellidocs-review-portal:p8` — Linux/amd64.

The API Linux build exposed an existing source-encoding problem in currency normalization. The Euro and Pound literals were changed to C# Unicode escapes (`\u20AC` and `\u00A3`), preserving P7 behavior while making compilation deterministic across the Windows and Linux SDK environments.

## Azure Container Apps boundary

Terraform now models optional Container Apps for:

- IntelliDocs API;
- IntelliDocs human-review portal.

Both use the existing Container Apps Environment.

The review portal receives the API FQDN through:

`ReviewApi__BaseUrl`

Application Container App deployment is explicitly opt-in:

`deploy_application_container_apps = false`

by default.

This prevents placeholder image references from changing the existing Azure development environment unless application deployment is intentionally enabled.

Terraform verification:

- `terraform fmt -check -recursive` — PASS;
- `terraform validate` — PASS;
- `deploy_application_container_apps` evaluated to `false`;
- Terraform state and saved plan files remain untracked.

No `terraform apply` was performed for the P8 application workloads.

Production identity, secret management, and authentication are intentionally deferred to P9. Private-reference networking is deferred to P10.

## Exit criterion

**PASS — Corrections auditable.**

P8 demonstrates that a low-confidence document can be claimed by a reviewer, corrected without overwriting the machine extraction, approved or rejected through deterministic state transitions, and reconstructed from persisted reviewer/correction/decision audit records.