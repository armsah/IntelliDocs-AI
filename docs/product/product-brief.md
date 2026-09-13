# IntelliDocs AI — Product Brief

## Purpose

IntelliDocs AI is an enterprise Intelligent Document Processing platform built on Microsoft Azure.

The platform receives business documents from customers and suppliers, classifies them, extracts structured information, validates business rules, routes uncertain cases to human reviewers, and publishes approved data to downstream systems.

The project demonstrates production-oriented AI engineering rather than custom model research.

## Primary Users

- Operations users submitting documents
- Human reviewers correcting uncertain extractions
- Administrators managing document policies
- Downstream enterprise systems consuming approved data
- Platform engineers operating and monitoring the solution

## Initial Document Types

The initial portfolio scope supports:

1. Invoice
2. Purchase Order
3. Delivery Note
4. Contract
5. Form
6. Unknown / unsupported document

Invoice, Purchase Order, and Delivery Note are the primary structured extraction types.

Contracts and Forms initially receive classification and OCR/layout support.

Unknown documents are routed to manual classification.

## Core Capabilities

- Secure document upload
- API submission
- OCR and layout analysis
- Document classification
- Key-value extraction
- Table extraction
- Confidence-aware routing
- Business-rule validation
- Duplicate detection
- Human review
- Review correction history
- Immutable audit events
- Downstream publication
- Reprocessing
- Operational monitoring
- Cost tracking

## Processing Principles

1. Job state is persisted in PostgreSQL.
2. Queue presence must never be treated as the source of truth for job state.
3. Event Grid signals storage events.
4. Service Bus owns durable processing semantics.
5. Processing handlers must be idempotent.
6. AI confidence cannot override deterministic business rules.
7. Every extracted field must retain source provenance.
8. Human corrections must record reviewer identity and timestamp.
9. AI integrations must be abstracted behind application interfaces.
10. Application workloads use managed identity in Azure.

## Target Azure Architecture

- Azure Front Door / WAF
- Azure API Management
- ASP.NET Core APIs
- Azure Blob Storage
- Azure Event Grid
- Azure Service Bus
- Azure AI Document Intelligence
- Azure Database for PostgreSQL
- Azure Container Apps
- Microsoft Entra ID
- Azure Key Vault
- Application Insights
- Azure Monitor
- OpenTelemetry
- Terraform
- GitHub Actions with OIDC

## P0 Exit Criterion

P0 is complete when a technical reviewer can understand:

- what IntelliDocs AI does;
- which documents are supported;
- how processing flows through the platform;
- how confidence and human review work;
- how document state is persisted;
- which non-functional targets apply;
- why Service Bus and Event Grid have separate responsibilities.
