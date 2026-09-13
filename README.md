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
- [ ] P1 — Local API and PostgreSQL state machine
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

**P0 — Architecture and product definition: complete**

P0 establishes the product scope, document schemas, confidence policy, non-functional requirements, target architecture, processing sequence, durable state machine, and major architectural decisions.

Next:

**P1 — Local ASP.NET Core upload/job API + PostgreSQL state machine**

P1 will establish an executable local application and prove the document lifecycle before Azure infrastructure is introduced.

## Repository Structure

```text
.github/
  workflows/
docs/
  adr/
  architecture/
  product/
  security/
evaluation/
infrastructure/
  terraform/
scripts/
src/
tests/
