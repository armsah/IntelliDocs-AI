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
- [ ] P2 — Blob ingestion and duplicate detection
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

**P1 — Local API and PostgreSQL state machine: complete**

P1 establishes the first executable IntelliDocs document lifecycle using ASP.NET Core .NET 10, Entity Framework Core, and PostgreSQL 18.

Implemented in P1:

- Multipart document upload and persisted DocumentJob creation
- SHA-256 content hashing
- Explicit durable document-processing state machine
- Append-only transition audit history
- PostgreSQL as the source of truth for processing state
- Temporary local filesystem document storage
- Document job and transition retrieval
- Development-only state-transition endpoint
- EF Core migration for the initial persistence model
- 8 unit tests and 2 PostgreSQL-backed integration tests

Automated verification: **10 tests total, 10 passed, 0 failed**.

SHA-256 is calculated in P1, but duplicate-safe ingestion is intentionally deferred to P2. Local filesystem storage is also temporary and will be replaced by Azure Blob Storage.

The PostgreSQL credentials in ppsettings.Development.json are local Docker development credentials only. Production identity and secret management are introduced in P9.

Next:

**P2 — Azure Blob Storage ingestion and duplicate-safe content handling**

## Local Development

Prerequisites: .NET 10 SDK, Docker Desktop, and Docker Compose.

Start PostgreSQL with docker compose up -d.

Apply migrations with dotnet ef database update --project src/IntelliDocs.Infrastructure --startup-project src/IntelliDocs.Api.

Run the API with dotnet run --project src/IntelliDocs.Api.

Run the complete automated test suite with dotnet test IntelliDocs.slnx.

The local PostgreSQL instance is exposed on port 5433.

### Local API

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | /health | Service health |
| POST | /api/v1/documents | Upload a document and create a persisted job |
| GET | /api/v1/documents/{documentId} | Retrieve job state and transition history |
| POST | /api/v1/documents/{documentId}/transitions | Development-only state-machine driver |
