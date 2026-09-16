# IntelliDocs AI Portfolio Demo Runbook

## Purpose

This runbook provides a concise portfolio demonstration of the IntelliDocs AI
document-processing architecture and its engineering controls.

A recorded video may follow this runbook, but this repository does not claim
that a demo video exists unless one is explicitly published.

## Recommended demo sequence

### 1. Architecture

Start with the repository architecture and explain the separation of
responsibilities:

- API accepts document work
- Blob Storage holds durable document content
- Event Grid signals change
- Service Bus owns durable work delivery, retry, and DLQ semantics
- the worker performs document analysis and deterministic routing
- PostgreSQL owns authoritative business state
- the review portal handles human corrections and decisions
- Azure Monitor/Application Insights provide operational and AI-quality
  visibility

Key design statement:

> Event Grid signals change; Service Bus owns work semantics.

### 2. Document ingestion

Show a synthetic document upload.

Explain:

- tenant-scoped document job
- SHA-256 content hashing
- durable Blob storage
- PostgreSQL state transition
- processing message publication

Do not use personal or confidential documents in the demo.

### 3. AI analysis and routing

Show the Document Intelligence result and explain:

- classification
- OCR/layout extraction
- normalized fields/tables
- model metadata
- deterministic confidence policy
- business validation
- automatic versus human-review routing

Emphasize that confidence does not directly make business decisions; the
policy layer converts model output into deterministic routing.

### 4. Human review

Show a document in the review portal.

Demonstrate:

- authenticated reviewer workflow
- extracted values
- correction
- approval or rejection
- persisted reviewer identity
- audit history

### 5. Failure recovery

Use the P5/P12 evidence rather than intentionally breaking shared Azure
resources during a portfolio demonstration.

Explain the demonstrated path:

`Processing -> retry -> DeadLettered -> re-drive -> Queued -> Processing -> Extracted`

Show the re-drive utility and explain the `r0 -> r1` generation change and
publish-before-DLQ-complete safety rule.

### 6. Observability

Show the P11 workbook-as-code and telemetry catalog.

Discuss:

- request reliability and latency
- processing outcomes
- routing decisions
- AI confidence
- validation issues
- review corrections/decisions
- retries and dead letters
- Document Intelligence operation count as a usage proxy

Do not describe the Document Intelligence operation counter as an Azure
billing meter.

### 7. Security and networking

Walk through the P9/P10 evidence:

- Entra authentication
- managed identities
- Key Vault
- no application client secret
- private-reference VNet topology
- Private Link paths for sensitive backends

Clearly distinguish IaC design/static validation from resources that have
actually been live-applied.

### 8. P12 engineering gates

Finish with:

- 65/65 .NET tests
- 21/21 Python evaluation tests
- standalone health HTTP 200
- 20/20 smoke requests successful
- 500/500 bounded-concurrency requests successful
- Terraform format/validation gate
- Git whitespace/secret hygiene gates

## Deployment boundary

The portfolio contains production-reference Azure architecture and Terraform,
but not every later phase has been live-applied.

P3/P4 infrastructure and Document Intelligence work have live Azure evidence.

P9/P10/P11 security, private networking, and observability additions were
validated primarily through code, tests, Terraform validation, and evidence
documents rather than a final full-environment Terraform apply.

P12 therefore must not be presented as a production load certification or a
claim that all private networking/dashboard resources are currently live.

## Demo safety

Use only synthetic documents.

Never display:

- connection strings
- passwords
- access tokens
- SAS values
- Document Intelligence keys
- Service Bus credentials
- PostgreSQL credentials

## Portfolio message

IntelliDocs AI demonstrates an enterprise document-processing design that
combines AI extraction with deterministic business controls, auditable human
review, durable asynchronous processing, identity-first security,
private-reference networking, observability, and tested failure recovery.