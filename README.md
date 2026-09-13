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
- [ ] P3 — Azure infrastructure with Terraform
- [ ] P4 — Azure AI Document Intelligence integration
- [ ] P5 — Service Bus worker, retries, DLQ and re-drive
- [ ] P6 — Classification/extraction evaluation
- [ ] P7 — Confidence policy and business validation
- [ ] P8 — Human-review portal
- [ ] P9 — Entra ID, managed identity and Key Vault
- [ ] P10 — Private-reference networking
- [ ] P11 — Observability, AI quality and cost metrics
- [ ] P12 — Load, failure and quality testing

## Current Phase

**P2 — Azure Blob Storage ingestion and duplicate-safe content handling: complete**

P2 replaces temporary local filesystem storage with Blob Storage and establishes tenant-scoped, concurrency-safe duplicate detection.

Implemented through P2:

- ASP.NET Core multipart document upload
- SHA-256 content hashing during ingestion
- Azure Blob Storage abstraction and implementation
- Azurite-backed Blob Storage for local development and integration testing
- Deterministic blob naming using `{tenantId}/{documentId:N}/{fileName}`
- Blob metadata containing `documentId`, `tenantId`, and `sha256`
- Content type preservation in Blob Storage
- Conditional blob creation to prevent accidental overwrite
- PostgreSQL-backed durable document-processing state machine
- Append-only document transition audit history
- PostgreSQL as the authoritative source of document-job state
- Tenant-scoped duplicate detection using `(tenantId, sha256)`
- Unique PostgreSQL constraint as the final duplicate-concurrency guard
- Database reservation before Blob upload so concurrent duplicate submissions cannot create duplicate blobs
- Same document content permitted across different tenants
- Document job and transition retrieval
- Development-only state-transition endpoint

The ingestion sequence reserves the `(tenantId, sha256)` document identity in PostgreSQL before uploading document bytes. A concurrent submission for the same tenant and content therefore loses at the database uniqueness boundary and returns HTTP 409 without creating another blob.

A successfully stored document transitions from `Submitted` to `Stored` after Blob upload. If Blob storage fails after the database reservation, the `Submitted` job remains durable for later recovery rather than losing the ingestion attempt.

P2 integration tests verify:

- uploaded bytes are persisted to Blob Storage
- stored bytes can be downloaded unchanged
- blob content type and metadata are preserved
- the Blob URI is persisted in PostgreSQL
- duplicate content within one tenant returns HTTP 409
- a duplicate submission leaves exactly one database job and one blob
- identical content across different tenants is accepted
- concurrent duplicate submissions create exactly one document job and one blob
- the P1 document lifecycle continues to operate with Blob-backed ingestion

Automated verification: **14 tests total, 14 passed, 0 failed**.

Local development uses Azurite and development-only connection strings. Production Azure identity, secret management, and managed identity are introduced in P9.

Next:

**P3 — Azure infrastructure with Terraform**

## Local Development

Prerequisites:

- .NET 10 SDK
- Docker Desktop
- Docker Compose

Start the local dependencies:

```powershell
docker compose up -d
```
