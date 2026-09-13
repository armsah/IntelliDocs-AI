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
- Azure workloads use managed identity.
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

## Implementation Roadmap

- [x] P0 — Architecture and product definition
- [x] P1 — Local API and PostgreSQL state machine
- [x] P2 — Blob ingestion and duplicate detection
- [x] P3 — Azure infrastructure with Terraform
- [x] P4 — Azure AI Document Intelligence integration
- [ ] P5 — Service Bus worker, retries, DLQ and re-drive
- [ ] P6 — Classification/extraction evaluation
- [ ] P7 — Confidence policy and business validation
- [ ] P8 — Human-review portal
- [ ] P9 — Entra ID, managed identity and Key Vault
- [ ] P10 — Private-reference networking
- [ ] P11 — Observability, AI quality and cost metrics
- [ ] P12 — Load, failure and quality testing

## Current Phase

**P4 — Azure AI Document Intelligence integration: complete**

Next:

**P5 — Service Bus worker, retries, DLQ and re-drive**

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

During P3 validation, the subscription-specific PostgreSQL capability endpoint reported Flexible Server provisioning as restricted in Germany West Central, while West Europe supported the required PostgreSQL versions and SKU. The regional exception is therefore explicit in Terraform through `postgresql_location`.

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

P4 integrates the platform with Azure AI Document Intelligence and establishes the AI-provider boundary that later asynchronous workers will consume.

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

P4 deliberately does **not** invoke Document Intelligence directly from the document upload request.

The P2 upload flow continues to terminate at the durable:

```text
Stored
```

state.

P5 introduces Azure Service Bus and the worker responsible for asynchronous processing orchestration.

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
kind     = FormRecognizer
sku_name = S0
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

The sample runner requires the following environment variables:

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

Existing P1 and P2 lifecycle/storage integration tests remain green.

Current automated verification through P4:

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

After provisioning, Terraform state contained nine managed resources:

- Azure AI Document Intelligence account
- Azure Container Apps Environment
- Log Analytics Workspace
- PostgreSQL Flexible Server
- PostgreSQL database
- Resource Group
- Storage Account
- Blob container
- random resource suffix

The post-apply Terraform plan reported:

```text
No changes. Your infrastructure matches the configuration.
```

This confirms that the P4 infrastructure converged after the Document Intelligence resource was introduced.

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

Next:

**P5 — Service Bus worker, retries, DLQ and re-drive**

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

P3 and P4 intentionally use local Terraform state for development. Generated state files, plan files, `.terraform/`, and real `.tfvars` files must remain outside version control.

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

Current automated verification through P4:

**19 tests total, 19 passed, 0 failed.**

Build the Document Intelligence sample runner separately:

```powershell
dotnet build tools\IntelliDocs.DocumentIntelligence.Sample\IntelliDocs.DocumentIntelligence.Sample.csproj
```

---

## Security Roadmap

The current implementation is intentionally incremental.

Current development-stage controls include:

- durable PostgreSQL state
- private Blob container
- duplicate-safe ingestion
- credentials excluded from committed application configuration
- Terraform state and real variable files excluded from Git
- synthetic documents for committed AI-service evidence

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

Development-stage public endpoints and API-key authentication should not be interpreted as the final production security posture.

---

## Next Phase

P5 introduces the durable asynchronous processing backbone:

- Azure Service Bus
- .NET processing worker
- document processing messages
- idempotent processing-stage execution
- retries
- dead-letter handling
- re-drive
- controlled concurrency

The intended processing relationship remains:

```text
Blob Storage
     |
     v
Event Grid
     |
     v
Service Bus
     |
     v
.NET Worker
     |
     +--> Azure AI Document Intelligence
     |
     +--> PostgreSQL
```

Event Grid signals that storage changed.

Service Bus owns durable work-processing semantics.

PostgreSQL remains the authoritative source of document-job state.
