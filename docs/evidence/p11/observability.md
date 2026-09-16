# P11 — Observability, AI Quality, and Cost Visibility

## Objective

P11 adds operational observability, AI-quality telemetry, human-review signals, and cost/usage visibility to IntelliDocs AI.

The implementation uses OpenTelemetry instrumentation exported to workspace-based Azure Application Insights and Azure Monitor. An Azure Monitor Workbook is defined in Terraform so the operational view is reproducible as infrastructure as code.

P11 exit criterion: **Quality + ops visible**.

## Architecture

The P11 telemetry path is:

```text
IntelliDocs API --------\
                         \
IntelliDocs Worker -------> OpenTelemetry
                         /       |
Review Portal ----------/        |
                                 v
                    Application Insights
                                 |
                                 v
                     Log Analytics Workspace
                                 |
                                 v
                     Azure Monitor Workbook
```

The existing Log Analytics workspace remains the central Azure Monitor workspace.

Terraform adds a workspace-based Application Insights resource and an Azure Monitor Workbook covering operations, AI quality, human review, and processing/Document Intelligence usage.

The API, Worker, and Review Portal receive the Application Insights connection string through their Container Apps configuration.

## Application instrumentation

Shared application telemetry is defined in:

`src/IntelliDocs.Infrastructure/Observability/IntelliDocsTelemetry.cs`

The shared meter and activity source are named:

`IntelliDocs`

The following custom metrics are defined:

| Metric                                            | Purpose                                                          |
| ------------------------------------------------- | ---------------------------------------------------------------- |
| `intellidocs.documents.processed`                 | Documents reaching a terminal processing outcome                 |
| `intellidocs.document.processing.duration`        | End-to-end processing duration in seconds                        |
| `intellidocs.ai.classification.confidence`        | Classification confidence distribution                           |
| `intellidocs.ai.policy.confidence`                | Confidence used by deterministic routing                         |
| `intellidocs.routing.decisions`                   | Approved/review routing decisions                                |
| `intellidocs.validation.issues`                   | Business-validation issue counts                                 |
| `intellidocs.review.corrections`                  | Human-review corrections                                         |
| `intellidocs.review.decisions`                    | Completed human-review decisions                                 |
| `intellidocs.processing.retries`                  | Service Bus processing retries                                   |
| `intellidocs.processing.deadletters`              | Processing dead-letter outcomes                                  |
| `intellidocs.ai.document_intelligence.operations` | Document Intelligence operation count used as a usage/cost proxy |

## Operational visibility

The Worker emits processing telemetry for:

- successful document processing;
- end-to-end processing duration;
- Service Bus retries;
- final processing dead-letter outcomes;
- document classification;
- Document Intelligence analysis operations;
- deterministic validation and routing.

Application Insights request telemetry provides API request volume, failures, and request latency.

Together these signals make the following operational indicators observable:

- API throughput;
- API failures;
- API P95 request latency;
- processed-document volume;
- document-processing duration;
- processing retries;
- processing dead letters.

## AI-quality visibility

The processing pipeline records:

- classification confidence by document type;
- deterministic policy confidence by document type;
- routing decision by document type;
- validation issue count by severity and document type;
- human-review correction count;
- human-review decision count.

These signals make it possible to observe whether low-confidence or invalid documents are being routed to human review and whether reviewers are correcting extracted information.

They complement the offline P6 evaluation suite rather than replace it. P6 measures quality against a controlled evaluation dataset; P11 measures production-oriented runtime signals.

## Human-review telemetry

Review telemetry is emitted only after successful persistence.

A successful correction increments:

`intellidocs.review.corrections`

A completed review increments:

`intellidocs.review.decisions`

The telemetry intentionally does not include:

- reviewer object IDs;
- document IDs;
- filenames;
- original extracted values;
- corrected values;
- review reasons.

The PostgreSQL audit model remains the authoritative source for detailed review history. Azure Monitor receives aggregate operational signals rather than document-level review content.

## Cardinality and privacy policy

Custom metric dimensions must remain low-cardinality and non-sensitive.

Allowed dimensions include bounded values such as:

- `document.type`;
- `outcome`;
- `decision`;
- `severity`;
- `operation`.

The telemetry must not use high-cardinality or sensitive values as metric dimensions, including:

- document IDs;
- Service Bus message IDs;
- tenant IDs;
- reviewer identities;
- filenames;
- Blob URIs;
- extracted document values;
- corrected values;
- exception messages;
- arbitrary validation messages.

Detailed identifiers may continue to appear where required in application logs and the auditable persistence model, subject to the application's logging and retention controls, but they are not used as custom metric dimensions.

## Azure Monitor Workbook

Terraform defines the workbook:

`IntelliDocs - Operations, AI Quality and Cost`

The workbook contains three primary views.

### Operations

The operations query summarizes Application Insights request telemetry by hour:

```kusto
requests
| summarize
    Requests=count(),
    Failed=countif(success == false),
    P95DurationMs=percentile(duration, 95)
    by bin(timestamp, 1h)
| order by timestamp asc
```

This exposes request throughput, failed requests, and P95 request latency.

### AI quality and human review

The quality query selects the IntelliDocs AI, routing, and review metrics:

```kusto
customMetrics
| where name startswith "intellidocs.ai."
    or name startswith "intellidocs.routing."
    or name startswith "intellidocs.review."
| summarize
    Value=sum(valueSum),
    Samples=sum(valueCount)
    by name, bin(timestamp, 1h)
| order by timestamp asc
```

This gives a consolidated view of runtime AI-quality and human-review signals.

### Processing and Document Intelligence usage

The usage query tracks document processing and Document Intelligence operations:

```kusto
customMetrics
| where name == "intellidocs.ai.document_intelligence.operations"
    or name == "intellidocs.documents.processed"
| summarize
    Value=sum(valueSum)
    by name, bin(timestamp, 1d)
| order by timestamp asc
```

This provides processing-volume and Document Intelligence usage visibility.

## Cost interpretation

`intellidocs.ai.document_intelligence.operations` is a usage proxy. It counts application-level Document Intelligence analysis operations.

It is **not** an Azure invoice, billing meter, or monetary cost calculation.

The metric is useful for:

- correlating AI usage with document throughput;
- identifying changes in analysis volume;
- estimating workload growth;
- supporting later comparison with Azure Cost Management or billing exports.

Application Insights and Log Analytics ingestion also have cost implications. The P11 design therefore keeps custom metric dimensions bounded and avoids document-level cardinality.

Actual Azure charges remain authoritative only in Azure billing/cost data.

## Infrastructure as code

P11 extends Terraform with:

- workspace-based Application Insights;
- Azure Monitor Workbook;
- Application Insights connection-string injection into the API Container App;
- Application Insights connection-string injection into the Worker Container App;
- Application Insights connection-string injection into the Review Portal Container App.

The existing Log Analytics workspace remains configured with a 30-day retention period.

## Validation evidence

P11 static validation completed successfully:

- `terraform fmt` completed;
- `terraform validate` reported the configuration as valid;
- API project build succeeded;
- Worker project build succeeded;
- full .NET test suite: **64 passed, 0 failed**;
- Python evaluation suite: **21 passed**;
- `git diff --check` reported no whitespace errors.

The Python evaluation tests must be run from the `evaluation` directory because `intellidocs_eval` is a repository-local package under that directory.

Example:

```powershell
Push-Location "evaluation"
python -m pytest -q
Pop-Location
```

## Deployment-validation boundary

P11 does not claim live Azure Monitor telemetry or a live workbook deployment.

Terraform was formatted and validated, but P11 was not applied to the Azure environment as part of this phase validation.

This is intentional because the current Terraform plan also contains previously prepared P9/P10 identity, Container Apps, Private Link, and networking changes, and the P10 Service Bus Premium/private-network design has cost implications.

A later controlled deployment should verify:

1. Application Insights resource creation.
2. API, Worker, and Review Portal telemetry export.
3. Custom IntelliDocs metrics arriving in Azure Monitor.
4. Workbook queries returning runtime data.
5. request throughput/failure/latency panels.
6. classification and policy-confidence signals.
7. routing/review/correction signals.
8. retry and dead-letter signals.
9. Document Intelligence usage correlation.
10. telemetry ingestion and Azure cost data.

## Exit assessment

P11 provides reproducible observability for:

**Operations**

- throughput;
- request latency;
- failures;
- processing duration;
- retries;
- dead letters.

**AI quality**

- classification confidence;
- policy confidence;
- deterministic routing;
- validation issues;
- human-review corrections;
- review decisions.

**Cost/usage**

- processed-document volume;
- Document Intelligence operation volume;
- Application Insights/Log Analytics telemetry visibility.

**P11 exit criterion: PASS — Quality + ops visible through instrumented runtime signals and an Azure Monitor Workbook defined as code, with live Azure validation explicitly deferred until controlled deployment.**
