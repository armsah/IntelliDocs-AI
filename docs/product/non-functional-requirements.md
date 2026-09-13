# Non-Functional Requirements

## Availability

Target:

- 99.9% demo SLO for the processing API.

Evidence:

- availability dashboard;
- failure exercise.

## Throughput

The platform must support configurable asynchronous and batch processing.

Measure:

- documents per minute;
- pages per minute;
- processing latency.

Evidence:

- load-test report.

## Accuracy

Confidence policy operates per document type and field.

Measure:

- document classification accuracy;
- exact-match accuracy;
- field-level precision;
- field-level recall;
- confidence calibration.

Evidence:

- versioned labeled evaluation dataset;
- quality report.

## Security

Required controls:

- Microsoft Entra ID;
- managed identity;
- encrypted storage;
- least privilege;
- Key Vault;
- no application client secrets in Azure workloads.

Evidence:

- threat model;
- RBAC matrix;
- identity architecture.

## Resilience

Required behavior:

- idempotent processing;
- bounded retries;
- dead-letter queue;
- re-drive;
- duplicate detection;
- persisted state machine;
- recovery from transient AI failures.

Evidence:

- failure walkthrough.

## Auditability

Every material processing and review transition must be recorded.

Audit data includes:

- document ID;
- previous state;
- new state;
- actor;
- timestamp;
- reason;
- correlation/trace identifier when available.

Evidence:

- audit queries;
- human-review demonstration.

## Cost

Track:

- AI processing cost;
- storage cost;
- messaging cost;
- application compute cost;
- database cost.

Publish:

- estimated cost per 1,000 pages;
- estimated cost per 1,000 documents.

Evidence:

- FinOps notes;
- cost dashboard.

## Maintainability

The AI provider must be abstracted behind an interface.

Business validation and confidence routing must be independently testable.

Infrastructure must be reproducible through Terraform.
