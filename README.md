# IntelliDocs AI

Enterprise Intelligent Document Processing platform on Microsoft Azure.

IntelliDocs AI securely ingests business documents, classifies them, extracts structured information, validates AI confidence and business rules, routes uncertain cases to human reviewers, and publishes approved data to downstream enterprise systems.

## Supported Document Types

Initial scope:

- Invoice
- Purchase Order
- Delivery Note
- Contract
- Form
- Unknown / unsupported documents

Invoice, Purchase Order, and Delivery Note are the primary structured extraction types.

Contracts and Forms initially receive classification and OCR/layout processing.

Unknown documents are routed to manual classification.

## Target Architecture

The target platform uses:

- ASP.NET Core
- .NET worker services
- PostgreSQL
- Azure Blob Storage
- Azure Event Grid
- Azure Service Bus
- Azure AI Document Intelligence
- Azure API Management
- Azure Front Door / WAF
- Azure Container Apps
- Microsoft Entra ID
- Managed Identity
- Azure Key Vault
- Application Insights
- Azure Monitor
- OpenTelemetry
- Terraform
- GitHub Actions with OIDC
- Python evaluation tooling

## Architecture Principles

- PostgreSQL owns durable document-job state.
- Queue presence does not define processing state.
- Event Grid signals storage events.
- Service Bus owns durable work processing.
- Processing handlers are idempotent.
- AI confidence cannot override deterministic business rules.
- Extraction results retain page and field provenance.
- Human corrections are auditable.
- Azure workloads use managed identity in the target production architecture.
- Infrastructure is reproducible through Terraform.

A key architectural decision is:

> Event Grid signals change; Service Bus owns work semantics.

Document processing state remains durable in PostgreSQL rather than being inferred from transient infrastructure such as queue presence.

## Documentation

- [Product brief](docs/product/product-brief.md)
- [Document types and fields](docs/product/document-types.md)
- [Confidence policy](docs/product/confidence-policy.md)
- [Retention policy](docs/product/retention-policy.md)
- [Non-functional requirements](docs/product/non-functional-requirements.md)
- [C4 context](docs/architecture/c4-context.md)
- [C4 containers](docs/architecture/c4-container.md)
- [Processing sequence](docs/architecture/processing-sequence.md)
- [State machine](docs/architecture/state-machine.md)
- [ADR-001: Event Grid vs Service Bus](docs/adr/ADR-001-service-bus-vs-event-grid.md)
- [ADR-002: Document Intelligence strategy](docs/adr/ADR-002-document-intelligence-strategy.md)
- [P5 Service Bus failure/re-drive evidence](docs/evidence/p5/failure-demo.md)

## Implementation Roadmap

- [x] P0 — Architecture and product definition
- [x] P1 — Local API and PostgreSQL state machine
- [x] P2 — Blob ingestion and duplicate detection
- [x] P3 — Azure infrastructure with Terraform
- [x] P4 — Azure AI Document Intelligence integration
- [x] P5 — Service Bus worker, retries, DLQ and re-drive
- [x] P6 — Classification/extraction evaluation
- [x] P7 — Confidence policy and business validation
- [x] P8 — Human-review portal
- [x] P9 — Entra ID, managed identity and Key Vault
- [x] P10 — Private-reference networking
- [ ] P11 — Observability, AI quality and cost metrics
- [ ] P12 — Load, failure and quality testing

## Current Phase

**P10 — Private-reference networking: complete**

P10 defines the private-reference network boundary for the Azure-hosted IntelliDocs services.

The Terraform architecture now provides:

- an IntelliDocs virtual network with dedicated Container Apps and private-endpoint subnets
- Container Apps environment integration with the dedicated infrastructure subnet
- private endpoints for Blob Storage, Service Bus, Key Vault, Document Intelligence, and PostgreSQL
- private DNS zones and VNet links for all five sensitive backend services
- public network access disabled for the five sensitive PaaS dependencies
- Service Bus Premium to support the private endpoint architecture
- Terraform outputs for the VNet, subnets, and private endpoint addresses
- explicit separation between the authenticated reviewer-facing application edge and sensitive backend paths

P10 evidence, including the network diagram, sensitive-path matrix, DNS mapping, and deployment-validation boundary, is retained at `docs/evidence/p10/network.md`.

The Terraform configuration passes formatting and static validation. P10 does not claim that the private-reference topology has been applied or live-validated in Azure.

The P10 exit criterion — sensitive paths documented: **PASS**.

Next:

**P11 — Observability, AI quality and cost metrics**

---

## P0 — Architecture and Product Definition

P0 establishes the product scope, document taxonomy, confidence policy, retention policy, non-functional requirements, architecture diagrams, processing sequence, state machine, and major architectural decisions.

Primary structured extraction types:

- Invoice
- Purchase Order
- Delivery Note

Initial OCR/layout and classification types:

- Contract
- Form

Unsupported or unknown documents are routed to manual classification.

### Document Lifecycle

The durable processing lifecycle is:

```text
Submitted
  -> Stored
  -> Queued
  -> Processing
  -> Extracted
  -> Validating
  -> Approved
  -> Publishing
  -> Completed
```

Documents requiring intervention follow:

```text
Validating
  -> NeedsReview
  -> InReview
  -> Approved
```

or:

```text
InReview
  -> Rejected
```

Processing failures can transition to:

```text
Processing
  -> Failed
```

or:

```text
Processing
  -> DeadLettered
```

Failed or dead-lettered work can later be retried or re-driven into the processing workflow.

PostgreSQL is the source of truth for the document state machine.

### Default Confidence Policy

Initial policy:

- mandatory field confidence >= 0.90 and business rules pass: eligible for automatic approval
- confidence from 0.70 to 0.89: human review
- confidence below 0.70: human review with warning
- critical business-rule conflict: human review regardless of AI confidence
- unsupported or unknown document type: manual classification

These defaults are designed to become configurable and measurable during later evaluation phases.

---

## P1 — Local API and PostgreSQL State Machine

P1 establishes the local application foundation using ASP.NET Core, Entity Framework Core, and PostgreSQL.

The solution contains:

```text
src/
  IntelliDocs.Api/
  IntelliDocs.Core/
  IntelliDocs.Infrastructure/

tests/
  IntelliDocs.UnitTests/
  IntelliDocs.IntegrationTests/
```

P1 implements:

- document upload API
- SHA-256 calculation
- durable `DocumentJob` persistence
- PostgreSQL-backed state transitions
- transition audit history
- document/job retrieval
- development-only transition endpoint
- health endpoint
- OpenAPI support in Development
- unit tests
- PostgreSQL integration tests

The API exposes the initial document lifecycle without relying on an in-memory job model.

---

## P2 — Blob Ingestion and Duplicate Detection

P2 replaces local file persistence with durable Blob Storage ingestion.

The local development environment uses Azurite through Docker Compose.

P2 implements:

- `IDocumentStorage` abstraction
- Azure Blob Storage implementation
- deterministic blob naming
- SHA-256 content hashing
- Blob metadata
- content-type preservation
- PostgreSQL-backed duplicate detection
- concurrency-safe duplicate handling
- Blob and PostgreSQL integration tests

Blob names follow a deterministic structure based on tenant, document ID, and sanitized source filename.

Duplicate detection uses the logical boundary:

```text
tenantId + sha256
```

A unique PostgreSQL index enforces this boundary.

The ingestion sequence reserves the document in PostgreSQL before uploading the Blob. This prevents concurrent same-tenant duplicate submissions from creating multiple durable documents or orphan duplicate blobs.

The same content can still be uploaded independently by different tenants.

PostgreSQL remains authoritative for document state.

---

## P3 — Azure Base Infrastructure with Terraform

P3 introduces reproducible Azure infrastructure for the platform's base runtime and data services.

Provisioned with Terraform:

- Azure Resource Group
- Azure Storage Account
- private `documents` Blob container
- Azure Database for PostgreSQL Flexible Server 16
- `intellidocs` PostgreSQL database
- Log Analytics Workspace
- Azure Container Apps Environment
- deterministic project/environment naming with a random resource suffix
- common resource tags
- Terraform outputs for resource discovery

The primary Azure region is:

```text
germanywestcentral
```

Azure Database for PostgreSQL Flexible Server is provisioned in:

```text
westeurope
```

During P3 validation, the subscription-specific PostgreSQL capability endpoint reported Flexible Server provisioning as restricted in Germany West Central, while West Europe supported the required PostgreSQL versions and SKU.

The regional exception is therefore explicit in Terraform through `postgresql_location`.

PostgreSQL currently uses:

- PostgreSQL 16
- `B_Standard_B1ms`
- 32 GiB storage
- availability zone `3`
- 7-day backup retention
- public network access for the current development phase

Private networking is intentionally deferred to P10.

The Container Apps Environment is connected to the Log Analytics Workspace and explicitly declares its Consumption workload profile so the configuration converges without provider/API drift.

### P3 Terraform Validation

P3 was verified through a complete create, convergence, destroy, and recreate cycle.

Validation evidence:

- `terraform fmt` completed successfully
- `terraform validate` reported a valid configuration
- initial Terraform deployment succeeded
- post-apply Terraform plan converged to no changes
- all Terraform-managed resources were destroyed
- Terraform state was verified empty
- the Azure resource group was verified deleted
- a new plan from zero reported `8 to add, 0 to change, 0 to destroy`
- recreation completed with `8 added, 0 changed, 0 destroyed`
- the recreated state contained all eight expected Terraform resources
- the final Terraform plan reported no changes

This demonstrates that the P3 Azure environment can be recreated from zero from the committed Terraform configuration.

Terraform provider selections are captured in `.terraform.lock.hcl`.

Local Terraform state, generated plan files, `.terraform/`, and real `.tfvars` files are excluded from Git.

---

## P4 — Azure AI Document Intelligence

P4 integrates the platform with Azure AI Document Intelligence and establishes the AI-provider boundary consumed by asynchronous processing.

The implementation includes:

- an `IDocumentIntelligenceProvider` abstraction in the Core project
- an Azure implementation using `Azure.AI.DocumentIntelligence`
- `prebuilt-invoice` for structured invoice extraction
- `prebuilt-layout` for generic OCR, layout, and table extraction
- normalized analysis DTOs independent of the Azure SDK
- page and line provenance
- field bounding-region provenance
- table and table-cell bounding-region provenance
- field-level confidence values
- deterministic fake-provider tests
- Azure SDK result-mapping tests
- a reusable real-Azure sample runner

The provider boundary prevents Azure SDK types from becoming the platform's domain contract.

Normalized analysis results retain:

- selected model ID
- extracted document content
- pages
- lines
- page geometry
- extracted fields
- field confidence
- field bounding regions
- tables
- table cells
- table bounding regions
- table-cell bounding regions

P4 deliberately does not invoke Document Intelligence directly from the document upload request.

P5 introduces the Service Bus worker responsible for asynchronous AI-processing orchestration.

### Model Strategy

P4 uses two Azure prebuilt models.

For invoices:

```text
prebuilt-invoice
```

This model provides structured invoice extraction in addition to OCR/layout information.

For generic OCR and document layout:

```text
prebuilt-layout
```

The layout model establishes the initial processing path for document types that do not yet have a dedicated structured extraction model.

Custom classification or extraction models are intentionally deferred until P6, where evaluation evidence can determine whether they are necessary.

### Azure AI Resource

Terraform provisions an Azure AI Document Intelligence account using:

```text
kind = FormRecognizer
sku  = S0
```

The account is deployed in:

```text
germanywestcentral
```

P4 uses public service access and local API-key authentication for development-time integration testing.

This is an intentional intermediate security configuration.

P9 replaces application secrets with:

- Microsoft Entra ID
- managed identity
- Azure Key Vault
- least-privilege Azure RBAC

Application configuration uses:

```text
DocumentIntelligence__Endpoint
DocumentIntelligence__ApiKey
```

The committed `appsettings.json` contains empty values only.

Real credentials are supplied through environment configuration and are not stored in committed application settings.

### Provider Abstraction

The Core project defines the Document Intelligence provider contract and normalized domain-facing result types.

The Azure implementation resides in the Infrastructure project.

Conceptually:

```text
Application / Worker
        |
        v
IDocumentIntelligenceProvider
        |
        +-----------------------------+
        |                             |
        v                             v
AzureDocumentIntelligenceProvider   Fake Provider
        |
        v
Azure AI Document Intelligence
```

The fake provider allows deterministic automated testing without requiring Azure availability or consuming AI service capacity.

The Azure provider maps SDK-specific results into normalized IntelliDocs contracts.

### Provenance

Extraction provenance is retained so later validation and human-review phases can connect extracted information back to the source document.

Normalized output includes page numbers and polygon geometry for applicable:

- lines
- fields
- tables
- table cells

This establishes the data needed for later reviewer highlighting and field-level traceability.

### P4 Real-Azure Evidence

A synthetic invoice containing no real customer data was analyzed against the provisioned Azure AI Document Intelligence service.

The source document is:

```text
samples/synthetic/invoice-p4.png
```

#### `prebuilt-invoice`

Observed real-Azure result:

- 1 page analyzed
- 16 structured fields extracted
- 1 table extracted
- expected invoice number recognized
- expected fictional supplier recognized

The OCR content contained the expected synthetic invoice number:

```text
INV-2026-1001
```

The normalized output is retained at:

```text
docs/evidence/p4/prebuilt-invoice.json
```

#### `prebuilt-layout`

Observed real-Azure result:

- 1 page analyzed
- OCR/layout content extracted
- 1 table extracted
- expected invoice number recognized
- expected total recognized

The layout model returned no named invoice fields, which is expected because structured invoice-field extraction is handled by `prebuilt-invoice`.

The normalized output is retained at:

```text
docs/evidence/p4/prebuilt-layout.json
```

The real-Azure verification confirmed that both model paths successfully recognize expected content from the same controlled synthetic source document.

### Sample Runner

A reusable console sample is provided at:

```text
tools/IntelliDocs.DocumentIntelligence.Sample/
```

It accepts:

```text
<invoice|layout> <input-file> <output-json>
```

Example invoice analysis:

```powershell
dotnet run `
    --project tools\IntelliDocs.DocumentIntelligence.Sample\IntelliDocs.DocumentIntelligence.Sample.csproj `
    -- invoice `
    samples\synthetic\invoice-p4.png `
    docs\evidence\p4\prebuilt-invoice.json
```

Example layout analysis:

```powershell
dotnet run `
    --project tools\IntelliDocs.DocumentIntelligence.Sample\IntelliDocs.DocumentIntelligence.Sample.csproj `
    -- layout `
    samples\synthetic\invoice-p4.png `
    docs\evidence\p4\prebuilt-layout.json
```

The sample runner requires:

```text
DocumentIntelligence__Endpoint
DocumentIntelligence__ApiKey
```

Do not commit real service credentials.

### P4 Automated Tests

P4 adds tests covering:

- model selection for `prebuilt-invoice`
- model selection for `prebuilt-layout`
- unsupported model handling
- deterministic fake-provider output
- normalized field mapping
- confidence mapping
- page provenance
- bounding-region mapping
- Azure SDK result normalization

Existing lifecycle and storage integration tests remain green.

P4 verification completed with:

```text
19 tests total
19 passed
0 failed
```

### P4 Terraform Validation

P4 extends the P3 Terraform environment with the Azure AI Document Intelligence account.

The initial P4 infrastructure plan reported:

```text
1 to add, 0 to change, 0 to destroy
```

The apply completed with:

```text
1 added, 0 changed, 0 destroyed
```

After provisioning, Terraform state contained nine managed resources.

The post-apply Terraform plan reported no infrastructure differences.

### P4 Exit Criteria

P4 demonstrates:

- Azure AI Document Intelligence provisioned through Terraform
- `prebuilt-invoice` integration
- `prebuilt-layout` integration
- OCR against the real Azure service
- structured invoice-field extraction against the real Azure service
- table extraction
- normalized provider abstraction
- confidence mapping
- page and bounding-region provenance
- deterministic automated AI-provider testing
- sanitized real-Azure sample outputs
- Terraform convergence
- existing document lifecycle behavior preserved

P4 therefore satisfies the phase exit criterion:

> OCR/extraction works.

---

## P5 — Azure Service Bus Worker, Retry, DLQ and Re-drive

P5 introduces the durable asynchronous processing backbone for IntelliDocs AI.

The implementation includes:

- Azure Service Bus Standard namespace provisioned through Terraform
- `document-processing` queue
- PeekLock message processing
- explicit completion, abandonment, and dead-letter settlement
- maximum delivery count of 5
- Service Bus duplicate detection
- durable PostgreSQL processing state
- .NET background processing worker
- Blob document streaming into the worker
- Azure AI Document Intelligence invocation from the worker
- durable normalized analysis persistence
- deterministic failure injection for failure testing
- controlled Service Bus DLQ re-drive utility
- application-level processing idempotency
- generation-aware broker MessageIds

### P5 Processing Flow

Document ingestion now initiates an asynchronous processing workflow:

```text
Upload API
    |
    v
Blob Storage
    |
    v
PostgreSQL: Stored
    |
    v
PostgreSQL: Queued
    |
    v
Service Bus: document-processing
    |
    v
.NET Worker
    |
    +--> PostgreSQL: Processing
    |
    +--> Blob read
    |
    +--> Azure AI Document Intelligence
    |
    +--> DocumentAnalysisRecord
    |
    v
PostgreSQL: Extracted
```

PostgreSQL remains the authoritative source of document-job state.

Service Bus owns durable work delivery, retry, dead-letter, and re-drive semantics.

Queue presence is not treated as business state.

### Upload and Queue Publication

After successful Blob ingestion, the API:

1. persists the `Stored` transition
2. transitions the job to `Queued`
3. commits the authoritative PostgreSQL state
4. publishes the processing message to Service Bus
5. returns the created document response

The initial processing stage is:

```text
layout-extraction
```

If Service Bus publication fails, PostgreSQL remains authoritative and retains the queued job rather than pretending processing succeeded.

### Service Bus Infrastructure

Terraform provisions:

- Azure Service Bus Standard namespace
- `document-processing` queue

The queue is configured with:

```text
Lock duration: 1 minute
Maximum delivery count: 5
Duplicate detection: enabled
Duplicate-detection window: 10 minutes
Dead-letter expired messages: enabled
Default message TTL: 1 day
Maximum queue size: 1024 MiB
```

The worker uses PeekLock processing and disables automatic completion.

Successful work is explicitly completed.

Transient processing failures are explicitly abandoned so Service Bus can redeliver the message.

Terminal processing failures are explicitly dead-lettered.

### Message Contract and Idempotency

The application processing idempotency boundary is:

```text
documentId + processingStage
```

The stable application idempotency key therefore has the form:

```text
{documentId}:{processingStage}
```

Service Bus duplicate detection introduces an additional consideration for controlled re-drive.

If the broker MessageId remained identical during re-drive, Service Bus could suppress the replacement message during the duplicate-detection window.

The broker MessageId therefore includes a re-drive generation:

```text
{idempotencyKey}:r{redriveCount}
```

Initial delivery:

```text
{documentId}:layout-extraction:r0
```

First re-drive:

```text
{documentId}:layout-extraction:r1
```

The application idempotency key remains stable while each intentional re-drive receives a new broker generation.

This preserves application-level idempotency without blocking legitimate operational recovery.

### Worker Processing

`src/IntelliDocs.Worker` consumes the `document-processing` queue.

The worker uses:

- `AutoCompleteMessages = false`
- controlled concurrency
- explicit message settlement
- PostgreSQL-backed state validation
- durable Blob reads
- Azure AI Document Intelligence
- durable analysis persistence

For valid queued work, the worker:

1. deserializes the processing message
2. loads the authoritative document job from PostgreSQL
3. validates document and tenant identity
4. transitions `Queued -> Processing`
5. commits the `Processing` transition
6. opens the durable document from Blob Storage
7. invokes Azure AI Document Intelligence
8. persists the normalized analysis result
9. persists AI model metadata
10. transitions `Processing -> Extracted`
11. explicitly completes the Service Bus message

Already-processed work is treated as an idempotent no-op and completed rather than processed twice.

Malformed, invalid, or terminal work can be explicitly dead-lettered.

### Durable Analysis Records

P5 introduces durable `DocumentAnalysisRecord` persistence.

Each record contains:

- document ID
- analysis timestamp
- model ID
- model version when available
- normalized analysis JSON

This separates the durable AI result from the transient Service Bus message lifecycle.

The corresponding Entity Framework migration is included in the repository.

### Retry and Dead-Letter Behavior

The P5 failure policy is bounded.

For processing exceptions before the maximum delivery count:

```text
failure -> Abandon -> Service Bus redelivery
```

With a maximum delivery count of 5, the demonstrated sequence is:

```text
delivery 1 -> failure -> Abandon
delivery 2 -> failure -> Abandon
delivery 3 -> failure -> Abandon
delivery 4 -> failure -> Abandon
delivery 5 -> terminal processing failure
```

On the terminal failure:

1. the PostgreSQL job transitions from `Processing` to `DeadLettered`
2. the transition is committed
3. the Service Bus message is explicitly moved to the DLQ

The demonstrated dead-letter reason is:

```text
ProcessingFailed
```

This prevents poison work from retrying indefinitely.

### Deterministic Failure Injection

The worker contains development-time deterministic failure injection used to verify retry and DLQ behavior without depending on random external failures.

Failure injection targets a specific document through development configuration.

It occurs after the `Processing` state has been durably persisted and before Blob/AI processing.

No failure-injection document ID is committed in application configuration.

### Controlled DLQ Re-drive

The re-drive utility is located at:

```text
tools/IntelliDocs.ServiceBus.Redrive/
```

It accepts a document ID and coordinates recovery between PostgreSQL and the Service Bus DLQ.

For dead-lettered work, it:

1. loads the authoritative PostgreSQL job
2. locates the corresponding DLQ message
3. validates document and tenant identity
4. transitions PostgreSQL from `DeadLettered` to `Queued`
5. increments the re-drive generation
6. publishes the replacement active-queue message
7. completes the original DLQ message only after successful replacement publication

The ordering is deliberate.

The original poison message is not discarded before replacement work has been accepted by Service Bus.

The utility also supports restart-safe handling of a job already transitioned to `Queued`.

### P5 Failure and Recovery Evidence

P5 was exercised against the provisioned Azure Service Bus resource using the synthetic invoice and deterministic worker failure injection.

The initial message used generation:

```text
r0
```

Observed behavior:

```text
delivery 1 -> abandoned
delivery 2 -> abandoned
delivery 3 -> abandoned
delivery 4 -> abandoned
delivery 5 -> DeadLettered
```

PostgreSQL transitioned the job to:

```text
DeadLettered
```

The Service Bus message entered the DLQ with reason:

```text
ProcessingFailed
```

Failure injection was then disabled.

The re-drive utility successfully performed:

```text
DeadLettered
    |
    v
Queued
    |
    v
r1 replacement message
```

The original DLQ message was completed after successful publication of the replacement.

The worker then consumed the `r1` message and successfully performed:

```text
Queued
    |
    v
Processing
    |
    v
Blob read
    |
    v
Azure AI Document Intelligence
    |
    v
DocumentAnalysisRecord persisted
    |
    v
Extracted
    |
    v
Service Bus message completed
```

This demonstrates recovery of poison work rather than merely detecting failure.

Sanitized evidence is retained at:

```text
docs/evidence/p5/failure-demo.md
```

The evidence intentionally excludes Service Bus connection strings, Document Intelligence keys, PostgreSQL passwords, and other credentials.

### P5 Automated Tests

P5 extends the automated test suite to cover the asynchronous upload/messaging boundary.

The upload integration behavior verifies that an accepted document reaches `Queued` and publishes the initial `layout-extraction` processing message.

Message tests verify:

- initial `r0` generation
- stable application idempotency key
- queue publication behavior
- existing lifecycle behavior
- existing Blob persistence behavior
- duplicate safety

Current automated verification through P5:

```text
20 tests total
20 passed
0 failed
```

### P5 Terraform Validation

P5 extends the Azure environment with two resources:

- Azure Service Bus Standard namespace
- `document-processing` Service Bus queue

The initial P5 Terraform plan reported:

```text
2 to add, 0 to change, 0 to destroy
```

The apply completed successfully.

After P5, Terraform state contains eleven managed resources:

- Azure AI Document Intelligence account
- Azure Container Apps Environment
- Log Analytics Workspace
- PostgreSQL Flexible Server
- PostgreSQL database
- Resource Group
- Azure Service Bus namespace
- `document-processing` Service Bus queue
- Storage Account
- Blob container
- random resource suffix

The final Terraform plan reported no infrastructure differences.

### P5 Security Posture

P5 deliberately uses development-stage credentials supplied through environment variables for real-Azure testing.

Committed configuration does not contain real:

- Service Bus connection strings
- Document Intelligence API keys
- PostgreSQL passwords

This is an intermediate development configuration.

P9 replaces development credential handling with:

- Microsoft Entra ID
- managed identity
- Azure Key Vault
- least-privilege Azure RBAC

P10 introduces the private-reference networking design.

### P5 Exit Criteria

P5 demonstrates:

- durable asynchronous document processing
- Service Bus infrastructure provisioned through Terraform
- explicit PeekLock message settlement
- bounded retry behavior
- poison-message dead-lettering
- durable PostgreSQL `DeadLettered` state
- stable application idempotency
- generation-aware Service Bus duplicate handling
- controlled DLQ re-drive
- successful processing after re-drive
- durable AI-analysis persistence
- existing document lifecycle and Blob behavior preserved

P5 therefore satisfies the phase exit criterion:

> Poison work recoverable.

---

## P6 — Classification and Extraction Evaluation

P6 introduces evidence-driven document classification and extraction routing for the target IntelliDocs document types.

Supported target types:

- invoice
- purchase order
- delivery note
- contract
- form

### Evaluation Harness

P6 adds a reusable Python evaluation package under `evaluation/`.

The harness evaluates classification accuracy, field extraction coverage, raw and normalized exact match, missing and unexpected fields, per-field and per-document-type quality, and confidence calibration.

A dataset without classifier predictions is reported as classification **not evaluated**, rather than incorrectly receiving perfect classification accuracy.

### Model Selection

Evaluation showed that a custom extraction model is not currently justified for the synthetic P6 fixtures.

| Document type | Classification | Extraction |
| --- | --- | --- |
| Invoice | Custom classifier | `prebuilt-invoice` |
| Purchase Order | Custom classifier | `prebuilt-layout` + query fields |
| Delivery Note | Custom classifier | `prebuilt-layout` + query fields |
| Contract | Custom classifier | `prebuilt-layout` |
| Form | Custom classifier | `prebuilt-layout` |

Decision evidence is retained at `docs/evidence/p6/model-selection.md`.

### Invoice Evaluation

The synthetic P4 invoice baseline produced:

```text
field coverage:          0.9091
raw exact match:         0.9091
normalized exact match:  0.9091
```

The missing expected field was `currency`.

Invoices therefore continue to use `prebuilt-invoice`; currency normalization and business validation are handled in P7.

### Purchase Order Extraction

Configured query fields:

```text
purchaseOrderNumber
orderDate
buyerName
supplierName
currency
totalAmount
```

Observed synthetic evaluation:

```text
field coverage:          1.0000
raw exact match:         1.0000
normalized exact match:  1.0000
missing fields:          0
unexpected fields:       0
average confidence:      ~0.995
```

Evidence:

```text
docs/evidence/p6/query-purchase-order.json
docs/evidence/p6/purchase-order-query-report.json
docs/evidence/p6/purchase-order-query-report.md
```

### Delivery Note Extraction

Configured query fields:

```text
deliveryNoteNumber
deliveryDate
supplierName
customerName
```

Observed synthetic evaluation:

```text
field coverage:          1.0000
raw exact match:         1.0000
normalized exact match:  1.0000
missing fields:          0
unexpected fields:       0
average confidence:      ~0.995
```

Evidence:

```text
docs/evidence/p6/query-delivery-note.json
docs/evidence/p6/delivery-note-query-report.json
docs/evidence/p6/delivery-note-query-report.md
```

### Contract and Form Processing

Contract and Form fixtures are processed through `prebuilt-layout`, providing the classification plus OCR/layout behavior required by the current product scope.

### Custom Document Classifier

P6 trains the Azure AI Document Intelligence classifier:

```text
intellidocs-p6-classifier-v1
```

Classes:

```text
invoice
purchase_order
delivery_note
contract
form
```

Training corpus:

```text
5 documents per class
25 training documents total
```

Independent holdout corpus:

```text
2 documents per class
10 holdout documents total
```

Holdout documents remain separate from classifier training inputs.

### Classifier Training Preparation

P6 includes:

```text
tools/generate-p6-classifier-fixtures.ps1
tools/generate-p6-classifier-layout.ps1
```

Generated raw Layout companion JSON is reproducible training material and is not committed.

### Classifier Infrastructure

Terraform adds the private Blob container `classifier-training`.

After P6, the Terraform-managed environment contains twelve resources.

Development-time data-plane access used Azure RBAC and user-delegation SAS generation. Managed application identity and final secret handling remain P9 responsibilities.

### Classifier Training and Evaluation Utilities

Training utility:

```text
tools/IntelliDocs.DocumentClassifier.Train/
```

Evaluation utility:

```text
tools/IntelliDocs.DocumentClassifier.Evaluate/
```

Sanitized evidence:

```text
docs/evidence/p6/classifier-build.json
docs/evidence/p6/classifier-holdout-results.json
```

Observed synthetic holdout result:

```text
correct:   10
total:     10
accuracy:  1.0000
confidence range: approximately 0.760-0.818
```

The 100% synthetic result demonstrates correct integration and class separation for the controlled fixtures. It is not presented as representative production accuracy.

### Runtime Worker Integration

P6 integrates classification into the Service Bus worker.

The runtime path is now:

```text
Service Bus message
    |
    v
Blob read
    |
    v
Custom Document Classifier
    |
    v
DocumentAnalysisRouter
    |
    +--> invoice -> prebuilt-invoice
    +--> purchase_order -> prebuilt-layout + query fields
    +--> delivery_note -> prebuilt-layout + query fields
    +--> contract -> prebuilt-layout
    +--> form -> prebuilt-layout
```

The classified type is persisted through `DocumentJob.DetectedType`.

Classifier ID, classified type, classifier confidence, and normalized analysis are persisted together in the durable analysis JSON envelope.

P5 retry, dead-letter, re-drive, settlement, and PostgreSQL-state semantics remain unchanged.

### Query Fields

P6 extends `DocumentAnalysisRequest` with optional query fields.

The Azure provider enables the Document Intelligence `QueryFields` feature only when query fields are supplied.

The sample runner now supports:

```text
invoice
layout
query
```

### P6 Automated Verification

.NET verification:

```text
26 tests total
26 passed
0 failed
```

Python evaluation verification:

```text
21 tests total
21 passed
0 failed
```

Terraform validation completed successfully with:

```text
terraform fmt -check -recursive
terraform validate
```

`git diff --check` reported no whitespace errors; remaining messages were LF/CRLF normalization warnings only.

### Evidence Boundary

All committed P6 document samples and classifier fixtures are synthetic and contain no real customer information.

The current measurements establish implementation correctness, Azure service integration, routing behavior, evaluation-pipeline behavior, and controlled synthetic baselines.

They do **not** establish representative production accuracy.

A production-quality evaluation requires a sanitized, representative, held-out dataset covering realistic supplier/customer variation, layouts, scanners, image quality, languages, multi-page documents, missing fields, ambiguous documents, and previously unseen templates.

### P6 Exit Criteria

P6 demonstrates:

- reusable classification/extraction evaluation tooling
- normalized field-level quality metrics
- confidence calibration metrics
- real Azure query-field extraction
- evidence-driven model selection
- five-class custom Document Intelligence classification
- isolated training and holdout fixtures
- 10/10 synthetic held-out classification
- structured Purchase Order extraction
- structured Delivery Note extraction
- continued prebuilt Invoice extraction
- Contract and Form OCR/layout processing
- classifier-driven worker routing
- durable classified document type and classifier confidence
- no custom extraction model where evidence does not justify one
- explicit synthetic-vs-production evidence boundary
- Terraform-managed classifier training storage
- preservation of P5 retry/DLQ behavior

P6 therefore satisfies the phase exit criterion:

> Target document types are supported through evidence-driven classification and extraction routing.

---

## P7 - Confidence Policy and Business Validation

P7 converts P6 classification and extraction output into deterministic business decisions. AI confidence is retained as evidence, but confidence alone is never sufficient for approval.

### Confidence policy

The C# policy is implemented under `src/IntelliDocs.Core/Validation/`.

The routing thresholds remain aligned with P0:

| Policy confidence | Decision |
| --- | --- |
| `>= 0.90` | `Approved` only for a supported structured document with no blocking validation issue |
| `>= 0.70` and `< 0.90` | `NeedsReview` |
| `< 0.70` | `NeedsReview` with `LOW_CONFIDENCE` |

Policy confidence is the minimum of classifier confidence and the confidence values of present mandatory fields.

Missing mandatory fields remain explicit business-rule failures rather than artificial zero-confidence values. The P0 `0.90` auto-approval threshold was intentionally not lowered for the P6 classifier confidence distribution.

### Deterministic normalization

`DocumentFieldNormalizer` normalizes extracted values without inventing missing data. It handles German and English dates and numbers, explicit currency mappings, identifier casing, and whitespace.

### Business validation

`DocumentBusinessValidator` enforces the P0 mandatory fields for Invoice, Purchase Order, and Delivery Note documents.

Blocking validation includes missing mandatory fields, invalid dates, invalid total amounts, invalid currency codes, and negative total amounts. Negative totals produce a `Critical` issue.

Contracts, forms, unknown classifications, and unsupported types route to manual review.

### Worker routing

After classification and extraction, the worker normalizes fields, validates business rules, applies the confidence policy, persists the result, and routes the document.

Successful state paths are:

    Processing -> Extracted -> Validating -> Approved
    Processing -> Extracted -> Validating -> NeedsReview

`DocumentProcessingResult` persists `Classification`, `Analysis`, and `Validation` in the existing JSON analysis envelope. The validation result contains normalized fields, policy confidence, routing decision, and validation issues.

This extends the existing `jsonb` envelope, so P7 requires no database migration.

### Retry and DLQ correctness

P7 preserves the P5 at-least-once Service Bus semantics. Before failure routing, the worker reloads `DocumentJob` from PostgreSQL so unsaved in-memory validation transitions are not mistaken for durable state.

If final persistence fails, durable `Processing` remains eligible for retry or `DeadLettered`. If persistence succeeds but Service Bus completion fails, durable `Approved` or `NeedsReview` is retained instead of being falsely dead-lettered.

### Policy evidence

P7 tests cover confidence boundaries, mandatory fields, critical validation, manual-review types, normalization, successful validation routing, and the retained dead-letter path.

Complete .NET regression: 55 succeeded, 0 failed, 0 skipped.

Evidence: `docs/evidence/p7/policy-tests.md`

P7 exit criterion - deterministic routing backed by policy tests: **PASS**.

---

## Terraform

Terraform configuration is located in:

```text
infra/terraform/
```

Authenticate with Azure CLI and expose the active subscription to the AzureRM provider:

```powershell
az login

$env:ARM_SUBSCRIPTION_ID = az account show --query id --output tsv
```

Provide the PostgreSQL administrator password through an environment variable rather than committing it:

```powershell
$env:TF_VAR_postgresql_administrator_password = "<secure-password>"
```

The value above is a placeholder. Replace it with a securely generated password; do not use the literal placeholder against an Azure environment.

Initialize and validate the Terraform configuration:

```powershell
terraform -chdir="infra\terraform" init
terraform -chdir="infra\terraform" fmt -check -recursive
terraform -chdir="infra\terraform" validate
```

Review the infrastructure execution plan:

```powershell
terraform -chdir="infra\terraform" plan
```

Apply the infrastructure:

```powershell
terraform -chdir="infra\terraform" apply
```

After applying, verify convergence:

```powershell
terraform -chdir="infra\terraform" plan
```

A converged environment should report no infrastructure changes.

Destroy the development environment when required:

```powershell
terraform -chdir="infra\terraform" destroy
```

`terraform.tfvars.example` documents supported configuration values without containing real credentials.

P3 through P6 intentionally use local Terraform state for development.

Generated state files, plan files, `.terraform/`, and real `.tfvars` files must remain outside version control.

Remote state and CI/CD hardening can be introduced in a later infrastructure phase.

---

## Local Development

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- Docker Compose
- PostgreSQL/Azurite through the provided Docker Compose configuration
- Azure CLI for Azure infrastructure operations
- Terraform for Azure infrastructure operations

Start the local dependencies:

```powershell
docker compose up -d
```

The local development stack provides PostgreSQL and Azurite for API and integration-test execution.

Run the complete automated test suite:

```powershell
dotnet test IntelliDocs.slnx
```

Current automated verification through P6:

**.NET: 26 tests total, 26 passed, 0 failed.**

Python evaluation:

```powershell
python -m unittest discover `
    -s "evaluation\tests" `
    -p "test*.py" `
    -v
```

**Python evaluation: 21 tests total, 21 passed, 0 failed.**

Build the Document Intelligence sample runner separately when required:

```powershell
dotnet build tools\IntelliDocs.DocumentIntelligence.Sample\IntelliDocs.DocumentIntelligence.Sample.csproj
```

Build the Service Bus re-drive utility separately when required:

```powershell
dotnet build tools\IntelliDocs.ServiceBus.Redrive\IntelliDocs.ServiceBus.Redrive.csproj
```

### Development Configuration

The local API and worker use PostgreSQL and Azurite through development configuration.

Real Azure integration values are supplied through environment variables rather than committed secrets.

Document Intelligence:

```text
DocumentIntelligence__Endpoint
DocumentIntelligence__ApiKey
```

Service Bus:

```text
ServiceBus__ConnectionString
```

The re-drive utility uses:

```text
INTELLIDOCS_SERVICEBUS_CONNECTION_STRING
INTELLIDOCS_POSTGRESQL_CONNECTION_STRING
```

Do not commit real values for these settings.

---

## Security Roadmap

The implementation is intentionally incremental.

Current development-stage controls include:

- durable PostgreSQL state
- private Blob container
- duplicate-safe ingestion
- explicit Service Bus settlement
- bounded retries and DLQ handling
- credentials excluded from committed application configuration
- Terraform state and real variable files excluded from Git
- synthetic documents for committed AI-service evidence
- sanitized P5 failure/recovery evidence

Later phases introduce:

- Microsoft Entra authentication
- managed identity
- Azure Key Vault
- least-privilege RBAC
- private-reference networking
- API Management
- Front Door / WAF
- CI/CD through GitHub Actions OIDC
- expanded audit and observability controls

Development-stage public endpoints, local authentication, and API-key/connection-string integration should not be interpreted as the final production security posture.

---

## P9 — Identity and Secretless Authentication

P9 adds Microsoft Entra authentication, managed identities, Azure Key Vault integration, workload identity federation, and least-privilege Azure RBAC.

Reviewer-facing workflows require authentication, and reviewer identity is derived from the authenticated Entra principal rather than a caller-supplied reviewer string.

The Review Portal requests the delegated `Review.Access` scope when calling the protected API. Its production confidential-client credential uses a federated user-assigned managed identity with `SignedAssertionFromManagedIdentity` instead of an Entra application client secret.

The API and Worker use managed identity for supported Azure data-plane access. PostgreSQL runtime configuration is referenced through Azure Key Vault rather than embedded in deployed Container App environment-variable values.

P9 evidence, including the identity diagram, RBAC matrix, credential inventory, and validation boundary, is retained at `docs/evidence/p9/identity.md`.

The P9 exit criterion — no application client secret: **PASS**.

---
## Next Phase

**P11 — Observability, AI quality and cost metrics**

P11 will add operational and AI-quality observability around the processing pipeline.

Planned work includes:

- end-to-end application telemetry and distributed tracing
- processing latency, throughput, retry, DLQ, and review-routing metrics
- AI extraction and classification quality metrics
- confidence and human-review outcome monitoring
- cost-oriented Azure service metrics and operational dashboards
- evidence that makes runtime health, AI quality, and cost behavior observable

P12 will subsequently exercise the system with load, failure, and quality testing and complete the portfolio demo polish.
