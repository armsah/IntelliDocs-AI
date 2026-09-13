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
- [x] P3 — Azure infrastructure with Terraform
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

**P3 — Azure base infrastructure with Terraform: complete**

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

The primary Azure region is `germanywestcentral`.

Azure Database for PostgreSQL Flexible Server is provisioned in `westeurope`. During P3 validation, the subscription-specific PostgreSQL capability endpoint reported Flexible Server provisioning as restricted in Germany West Central, while West Europe supported the required PostgreSQL versions and SKU. The regional exception is therefore explicit in Terraform through `postgresql_location`.

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
- the recreated state contains all eight expected Terraform resources
- the final Terraform plan reported no changes

This demonstrates that the P3 Azure environment can be recreated from zero from the committed Terraform configuration.

Terraform provider selections are captured in `.terraform.lock.hcl`. Local Terraform state, generated plan files, `.terraform/`, and real `.tfvars` files are excluded from Git.

Next:

**P4 — Azure AI Document Intelligence integration**

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

P3 intentionally uses local Terraform state. Generated state files, plan files, `.terraform/`, and real `.tfvars` files must remain outside version control. Remote state and CI/CD hardening can be introduced in a later infrastructure phase.

## Local Development

Prerequisites:

- .NET 10 SDK
- Docker Desktop
- Docker Compose

Start the local dependencies:

```powershell
docker compose up -d
```

The local development stack provides PostgreSQL and Azurite for API and integration-test execution.

Run the complete automated test suite:

```powershell
dotnet test IntelliDocs.slnx
```

Current automated verification through P3: **14 tests total, 14 passed, 0 failed**.
