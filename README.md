IntelliDocs AI

Enterprise document intelligence reference implementation for secure
ingestion, AI-assisted classification/extraction, deterministic
confidence-based routing, auditable human review, resilient asynchronous
processing, and Azure-ready operations.

Project status: P0--P12 complete --- portfolio-ready reference
implementation.
Final validated commit: 273f11d
(P12: add load resilience quality and demo evidence)

1. Problem and business scenario

German and European enterprises still receive operational documents
through heterogeneous channels and formats: invoices, purchase orders,
delivery notes, contracts, forms, PDFs, scans, and images. Manual
classification and key-field extraction are slow, inconsistent,
difficult to audit, and expensive to scale.

IntelliDocs AI models a production-oriented document-processing
platform that:

accepts documents through an ASP.NET Core API;

persists an authoritative processing state in PostgreSQL;

stores source documents in Azure Blob Storage;

uses Azure Service Bus for durable asynchronous work;

invokes Azure AI Document Intelligence for OCR/layout analysis;

classifies supported document types and normalizes extracted fields;

applies deterministic confidence and business-validation rules;

routes uncertain or invalid results to an auditable human-review
workflow;

supports bounded retry, dead-lettering, and controlled re-drive;

exposes operational, AI-quality, review, and usage telemetry;

uses Terraform to define the Azure reference architecture.

The system is designed around a simple business principle:

AI proposes; deterministic policy decides whether the result can
proceed automatically or requires review.

2. 30-second architecture diagram

flowchart LR
U[User / Client]
FD[Front Door + WAF\nproduction reference]
APIM[API Management\nproduction reference]
ID[Microsoft Entra ID]
API[ASP.NET Core\nUpload API]
BLOB[Azure Blob Storage]
EG[Event Grid\nchange signal]
SB[Azure Service Bus\nwork queue + DLQ]
W[.NET Worker]
DI[Azure AI\nDocument Intelligence]
PG[(PostgreSQL)]
RP[Blazor\nReview Portal]
MON[Application Insights\nAzure Monitor]
KV[Key Vault]

    U --> FD --> APIM --> API
    ID --> API
    ID --> RP
    API --> BLOB
    API --> PG
    BLOB --> EG
    API --> SB
    EG -. change signal .-> SB
    SB --> W
    W --> DI
    W --> PG
    RP --> API
    KV -. secrets/config .-> API
    KV -. secrets/config .-> W
    API --> MON
    W --> MON
    RP --> MON

Core design decision: Event Grid signals change; Service Bus owns
durable work semantics---delivery, retry, dead-lettering, concurrency,
and re-drive.

The production-reference network places API, worker, and review
workloads in a Container Apps VNet integration subnet and sensitive
Azure dependencies behind Private Endpoints and Private DNS.

3. AI/ML scope and explicit non-goals

In scope

document-type classification for the supported synthetic evaluation
set;

OCR/layout extraction through Azure AI Document Intelligence;

normalized field extraction;

confidence-aware routing;

business-rule validation;

human correction of uncertain or invalid AI output;

AI model ID/version capture with analysis records;

repeatable quality evaluation and regression testing.

Explicit non-goals

This repository does not claim:

general-purpose document understanding for arbitrary document
families;

production accuracy based on the synthetic evaluation set;

autonomous approval of business documents solely from model
confidence;

LLM-based free-form decision making;

representative production-scale load certification;

automated model retraining from reviewer corrections;

that every production-reference P9--P11 Azure component has been
live-deployed.

The controlled fixtures demonstrate integration correctness and class
separation. A production model would require a sanitized,
representative, held-out corpus covering suppliers/customers, layouts,
scanners, image quality, languages, multi-page documents, missing
fields, ambiguous documents, and unseen templates.

4. Engineering highlights

Durable state machine: PostgreSQL is the authoritative
document/job state store.

Idempotent ingestion: content hashing and tenant-aware duplicate
handling.

Asynchronous processing: Service Bus decouples upload latency
from AI processing.

At-least-once-safe workflow: durable state is reloaded before
failure routing.

Bounded failure handling: retry → terminal failure → DLQ →
controlled re-drive.

Deterministic confidence policy: routing is testable and
independent of subjective model behavior.

Auditable human review: decisions and field corrections are
persisted with reviewer identity and timestamps.

Identity-first Azure design: Entra authentication, managed
identities, Key Vault, and RBAC; no application client secret in the
target architecture.

Private-reference networking: VNet, Private Link, Private DNS,
and disabled backend public access in Terraform.

Low-cardinality telemetry: operations, AI quality, routing,
review, retry/DLQ, and usage signals.

Infrastructure as code: reproducible Azure resources using
Terraform.

Portfolio verification: .NET, Python evaluation, Terraform
validation, runtime health, local load probes, failure evidence, and
demo runbook.

5. Architecture and service-selection decisions

Concern Decision Rationale

API ASP.NET Core Strong .NET/Azure
integration,
testability, typed
contracts

Durable state PostgreSQL Transactional workflow
state, audit queries,
portable relational
model

Source documents Azure Blob Storage Durable object storage
suited to PDFs/images

Work orchestration Azure Service Bus PeekLock, retries, DLQ,
duplicate detection,
explicit settlement

Change notification Event Grid Event notification
without making it the
durable work queue

OCR/layout Azure AI Document Managed document
Intelligence OCR/layout capability

Worker .NET background worker Explicit control over
processing, retry,
persistence, telemetry

Review UI Blazor Shared .NET stack and
authenticated internal
workflow

Identity Microsoft Entra ID + Avoid embedded
managed identity application credentials

Secret store Azure Key Vault Centralized
secret/configuration
boundary

Networking VNet + Private Link + Production-reference
Private DNS isolation of sensitive
backend paths

Hosting Azure Container Apps Managed container
runtime with VNet
integration

Observability OpenTelemetry + Standard
Application instrumentation plus
Insights/Azure Monitor Azure-native operations

IaC Terraform Reproducibility,
reviewable
infrastructure changes

Why Service Bus rather than Event Grid for processing?

Event Grid is optimized for event notification. The document pipeline
requires explicit settlement, retry, dead-lettering, delivery-count
behavior, duplicate handling, and controlled re-drive. Those are Service
Bus responsibilities.

Why PostgreSQL remains authoritative

Broker delivery state is not business state. The worker reloads the
current DocumentJob before making durable workflow decisions so
at-least-once message delivery cannot silently replace the database
state machine.

6. Data/model/service quality metrics

Quality is evaluated at multiple layers rather than represented by one
aggregate "AI accuracy" number.

Automated quality gates

Gate Result

.NET unit + integration tests 65 / 65 passed
Python AI/document evaluation tests 21 / 21 passed
Terraform fmt -check PASS
Terraform validate PASS
Git whitespace/diff gate PASS
API health regression PASS

The evaluation harness covers:

classification accuracy;

extraction coverage;

raw exact match;

normalized exact match;

missing/unexpected fields;

per-field quality;

per-document-type quality;

confidence calibration.

The synthetic fixtures achieved the expected controlled evaluation
result, but those results are not presented as representative
production accuracy.

Confidence policy

The P0 auto-approval threshold is 0.90. Missing mandatory fields
remain explicit validation failures rather than being converted into
artificial zero-confidence values. Routing combines model confidence
with deterministic business validation.

7. Security and privacy model

The target production architecture follows identity-first and
least-privilege principles.

Authentication and authorization

Microsoft Entra ID protects API/review access.

The API uses Microsoft Identity Web for JWT bearer authentication.

The review portal uses Entra OpenID Connect.

Reviewer identity comes from the authenticated principal (oid)
rather than a client-supplied reviewer field.

Azure workloads use managed identity where supported.

Terraform defines RBAC assignments for workload-to-resource access.

The portal-to-API credential design uses workload
identity/federation rather than an application client secret.

Secrets

Azure Key Vault is the target secret boundary.

Secrets, access keys, SAS values, and tokens must not be committed
or displayed in demos.

Local development configuration is separate from the production
identity model.

Network isolation

The production-reference Terraform defines:

VNet 10.40.0.0/16;

Container Apps infrastructure subnet 10.40.0.0/23;

Private Endpoint subnet 10.40.2.0/24;

Private Endpoints for Blob, Service Bus, Key Vault, Document
Intelligence, and PostgreSQL;

Private DNS zones for each sensitive backend;

disabled backend public access in the target configuration.

Telemetry privacy

Custom metrics use bounded, low-cardinality dimensions such as:

document type;

outcome;

decision;

severity;

operation.

Telemetry deliberately avoids document IDs, reviewer IDs, filenames,
extracted business values, and exception messages as metric dimensions.

8. Reliability/failure model

The processing path is designed for at-least-once delivery, not
exactly-once assumptions.

Queued
-> Processing
-> Extracted / review routing
-> transient failure -> Abandon -> redelivery
-> terminal failure -> DeadLettered
DeadLettered
-> controlled re-drive
-> Queued
-> Processing

Service Bus semantics

PeekLock processing;

explicit completion/abandon/dead-letter settlement;

bounded delivery attempts;

maximum demonstrated delivery count: 5;

duplicate detection window in the Azure configuration;

application-level idempotency based on document/work stage;

generation-specific broker MessageId for re-drive.

Demonstrated failure sequence

The P5 Azure failure exercise demonstrated:

Queued
-> Processing
-> retry
-> DeadLettered
-> re-drive
-> Queued
-> Processing
-> Extracted

The re-drive tool:

loads authoritative PostgreSQL state;

requires an eligible dead-lettered/re-drive state;

locates the matching DLQ message;

validates document and tenant identity;

increments RedriveCount;

transitions durable state back to Queued;

publishes a generation-specific replacement message;

completes the original DLQ message only after successful
publication.

This minimizes message-loss risk during recovery.

9. Human review / fallback strategy

Documents that cannot safely auto-progress are routed to human review
based on deterministic confidence and validation policy.

The review workflow supports:

review creation for routed documents;

authenticated reviewer identity;

inspection of extracted analysis;

field-level corrections;

approval/rejection decisions;

decision reasons;

timestamps;

persisted correction history;

document-state transitions;

review/correction telemetry.

The design treats human review as a first-class business workflow, not
an exception hidden inside the AI layer.

10. Observability and SLOs

Telemetry

The shared IntelliDocs OpenTelemetry meter/activity source emits
signals for:

processed documents;

processing duration;

classification confidence;

policy confidence;

routing decisions;

validation issues;

review corrections;

review decisions;

processing retries;

dead letters;

Document Intelligence operations.

A workspace-based Application Insights resource and Azure Monitor
workbook are defined in Terraform. The workbook covers operational
health, AI quality, review behavior, and usage/cost proxies.

Operational indicators

The project exposes the signals needed to reason about:

API availability and latency;

document throughput;

processing latency;

failure/retry/DLQ rates;

AI confidence distribution;

validation severity;

auto-route vs review-route distribution;

correction/review outcomes;

Document Intelligence operation volume.

SLO position

This portfolio implementation establishes SLO-ready telemetry, but
it does not claim production SLO attainment from local tests. Production
targets should be established after representative workload and Azure
deployment measurements.

Candidate production SLOs include API availability, end-to-end
processing latency, DLQ rate, review backlog age, and successful
processing ratio.

11. MLOps/model versioning and rollback

Each analysis can retain the AI model identifier and model version
alongside the document-processing record. This provides the minimum
traceability needed to correlate outputs and quality metrics with a
model generation.

The evaluation suite provides a regression gate before changing
classification/extraction behavior.

A production MLOps extension should add:

versioned representative datasets;

immutable model artifacts/configuration;

per-version offline quality reports;

approval gates before promotion;

canary/shadow evaluation;

drift monitoring;

explicit rollback to the previous approved model/configuration;

reviewer-correction feedback pipelines with governance.

Automated retraining and automatic promotion are intentionally outside
the current scope.

12. Deployment and CI/CD

Infrastructure is defined under infra/terraform.

The Azure reference architecture includes:

resource group;

Storage;

PostgreSQL Flexible Server;

Service Bus;

Azure AI Document Intelligence;

Log Analytics;

Application Insights;

Container Apps environment/workloads;

Entra application registrations and identity configuration;

managed identities and RBAC;

Key Vault;

VNet/subnets;

Private Endpoints;

Private DNS;

Azure Monitor workbook.

The intended CI/CD security model is GitHub Actions → OIDC/federated
Azure identity, avoiding long-lived Azure deployment credentials.

Deployment-validation boundary

P3/P4 include live Azure infrastructure/Document Intelligence evidence.
Later P9/P10/P11 production-reference identity, networking, and
observability work was validated primarily through implementation,
automated tests, Terraform formatting/validation, and committed evidence
rather than a final full Azure apply.

Do not interpret static Terraform validation as proof that every
production-reference Azure path is currently live.

13. How to run locally

Prerequisites

.NET 10 SDK;

Docker Desktop / Docker Compose;

PostgreSQL development container;

Azurite for local Blob Storage;

Python 3 + pytest;

Terraform for infrastructure validation.

Start local dependencies

Start the repository's Docker-based development dependencies. The
established local environment uses:

PostgreSQL exposed on host port 5433;

Azurite Blob service on the standard development ports.

Run the API

In PowerShell:

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5000"

dotnet run `  --project ".\src\IntelliDocs.Api\IntelliDocs.Api.csproj"`
--no-launch-profile

Health check:

Invoke-WebRequest `  -UseBasicParsing`
-Uri "http://127.0.0.1:5000/health"

Expected payload:

{
"status": "healthy",
"service": "IntelliDocs.Api"
}

Run the full verification gate

.\scripts\p12\verify.ps1

Or run quality regression only:

.\scripts\p12\quality-regression.ps1

The verification script covers .NET build/tests, Python evaluation,
Terraform formatting/validation, and Git diff hygiene.

14. How to deploy a low-cost Azure demo

The full production-reference topology should not be applied merely
for a portfolio demo. In particular, Service Bus Premium and Private
Link/private networking can materially increase cost.

For a low-cost demonstration:

use a dedicated disposable Azure resource group;

deploy only the minimum resources needed for the scenario;

prefer development/consumption-compatible SKUs where the feature set
permits;

demonstrate Azure AI Document Intelligence with synthetic documents;

keep the API/worker local or use a minimal Container Apps deployment
if live hosting is required;

use managed identity/RBAC where the chosen demo topology supports
it;

omit the full Private Link topology from the inexpensive demo and
present it as the committed production-reference design;

set a budget/cost alert before the demo;

run the documented walkthrough;

destroy the demo resources immediately afterward.

Before any terraform apply, review the selected SKUs and planned
changes. The committed production-reference Terraform includes
components intended to demonstrate architecture, not to minimize
portfolio-demo spend.

15. Test and benchmark results

Final regression

.NET tests: 65 passed / 65
Python tests: 21 passed / 21
Terraform fmt: PASS
Terraform validate: PASS
Git diff check: PASS
API health: HTTP 200

Local bounded-concurrency probe

Target: local /health endpoint.

Requests Concurrency Success Failure Success Elapsed RPS P50 P95 P99 Max
rate

      20             2        20         0      100%    0.83 s   24.07   2.11   84.11 88.66 ms 88.66 ms
                                                                           ms      ms

     500            20       500         0      100%   45.79 s   10.92   3.77   26.40   662.57   750.84
                                                                           ms      ms       ms       ms

Interpretation: the primary result is 520/520 successful local
requests. This is a bounded-concurrency reliability/performance probe,
not a production capacity benchmark. PowerShell Start-Job overhead,
local execution, and the lightweight health endpoint materially affect
throughput and tail latency.

16. Known trade-offs / production improvements

Current choice Trade-off Production improvement

Synthetic evaluation Repeatable but not Build sanitized,
corpus representative held-out enterprise
corpus

Local health load probe Good smoke signal, weak k6/Locust/Azure Load
capacity evidence Testing against deployed
workload

Deterministic Explainable and Calibrate per document
thresholds testable type/field using
production evidence

Manual review workflow Safe fallback Add queues, SLAs,
assignment, escalation,
reviewer analytics

DI operation count as Useful operationally, Join Azure Cost
usage proxy not billing truth Management/exported
billing data

Static validation of Safe/cost-aware Run controlled staging
later Azure phases deployment and
integration suite

Service Bus Premium for Strong isolation, Separate low-cost demo
Private Link reference higher demo cost and production Terraform
profiles

Basic model Supports attribution Add model registry,
traceability promotion gates, canary
and rollback automation

No automated retraining Avoids unsafe feedback Governed
loops correction-to-training
pipeline

17. Demo walkthrough

A concise portfolio walkthrough:

Problem: explain the manual enterprise-document processing
bottleneck.

Architecture: show the 30-second diagram and the Event Grid vs
Service Bus responsibility split.

Ingestion: submit a synthetic document and show durable
PostgreSQL state plus Blob storage.

AI analysis: show Document Intelligence output, classification,
normalized extraction, and captured model metadata.

Policy: demonstrate confidence/business validation and
deterministic routing.

Human review: correct a field and record an auditable decision.

Failure recovery: explain or replay the retry → DLQ → re-drive →
successful processing path.

Security: show Entra/managed-identity/Key-Vault design and
private-reference network diagram.

Observability: show the telemetry catalog/workbook definition
and explain quality/operations/cost signals.

Verification: run scripts/p12/verify.ps1 and show the passing
engineering gates.

Boundary: clearly distinguish live-tested components from
production-reference IaC that was statically validated.

Detailed walkthrough: docs/evidence/p12/demo-runbook.md.

Use synthetic documents only and never display credentials, keys,
tokens, connection strings, or SAS values.

18. Resource cleanup

Local

Stop local application processes and remove disposable
containers/volumes according to the repository's Docker configuration.

Before deleting volumes, confirm that no local development data needs to
be retained.

Azure demo

For a disposable demo resource group, the safest cleanup is to destroy
only resources that belong exclusively to that demo.

If the environment was created from an isolated Terraform state:

terraform -chdir="infra\terraform" plan -destroy

Review the plan carefully before executing any destroy operation.

If a dedicated resource group was used, verify its contents before
deleting it. Never run destructive commands against a shared
subscription/resource group without confirming resource ownership.

After cleanup, verify:

Container Apps/demo compute is removed;

Service Bus resources are removed if no longer needed;

Document Intelligence demo resources are removed if disposable;

PostgreSQL and Storage resources are removed only when their data is
no longer required;

Private Endpoints/DNS links are removed with the disposable
environment;

Key Vault soft-delete behavior is understood;

no unexpected billable resources remain.

Evidence map

Phase Evidence

P4 Azure Document Intelligence integration evidence
P5 docs/evidence/p5/failure-demo.md
P9 docs/evidence/p9/identity.md
P10 docs/evidence/p10/network.md
P11 docs/evidence/p11/observability.md
P12 docs/evidence/p12/load-test.md
P12 docs/evidence/p12/failure-recovery.md
P12 docs/evidence/p12/quality-regression.md
P12 docs/evidence/p12/demo-runbook.md

Final status

P0--P12 complete.

IntelliDocs AI is a portfolio-ready Azure/.NET document-intelligence
reference implementation demonstrating secure ingestion, AI-assisted
extraction, deterministic policy, human review, resilient messaging,
auditable state, infrastructure as code, and operational/quality
telemetry---with explicit boundaries between validated implementation
evidence and production deployment claims.
