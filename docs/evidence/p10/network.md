# P10 Network Evidence

## Objective

P10 establishes the private-reference network architecture for IntelliDocs AI.

The reviewer-facing application ingress remains an explicit application edge. Sensitive backend service paths are represented with Azure Private Link, private endpoints, private DNS, and an Azure Container Apps environment integrated with the IntelliDocs virtual network.

This phase defines and statically validates the Terraform configuration. It does not claim that the P10 topology has been applied or live-validated in Azure.

## Network Architecture

```mermaid
flowchart TB
    Reviewer["Reviewer / Browser"]

    subgraph Edge["Authenticated application edge"]
        Portal["Blazor Review Portal"]
        API["ASP.NET Core API"]
    end

    subgraph VNet["IntelliDocs VNet - 10.40.0.0/16"]
        subgraph CASubnet["Container Apps subnet - 10.40.0.0/23"]
            Portal
            API
            Worker[".NET Processing Worker"]
        end

        subgraph PESubnet["Private endpoint subnet - 10.40.2.0/24"]
            BlobPE["Blob Private Endpoint"]
            ServiceBusPE["Service Bus Private Endpoint"]
            KeyVaultPE["Key Vault Private Endpoint"]
            DIPE["Document Intelligence Private Endpoint"]
            PostgreSQLPE["PostgreSQL Private Endpoint"]
        end
    end

    Blob["Azure Blob Storage"]
    ServiceBus["Azure Service Bus Premium"]
    KeyVault["Azure Key Vault"]
    DI["Azure AI Document Intelligence"]
    PostgreSQL["Azure Database for PostgreSQL"]

    Reviewer -->|"HTTPS + Entra authentication"| Portal
    Portal -->|"HTTPS / delegated API token"| API

    API --> BlobPE
    API --> ServiceBusPE
    API --> KeyVaultPE
    API --> PostgreSQLPE

    Worker --> BlobPE
    Worker --> ServiceBusPE
    Worker --> KeyVaultPE
    Worker --> DIPE
    Worker --> PostgreSQLPE

    BlobPE --> Blob
    ServiceBusPE --> ServiceBus
    KeyVaultPE --> KeyVault
    DIPE --> DI
    PostgreSQLPE --> PostgreSQL

## Network Segmentation

| Segment | CIDR | Purpose |
|---|---|---|
| IntelliDocs VNet | `10.40.0.0/16` | Private-reference network boundary |
| Container Apps infrastructure subnet | `10.40.0.0/23` | Dedicated delegated subnet for the Container Apps environment |
| Private endpoint subnet | `10.40.2.0/24` | Dedicated subnet for Private Link endpoints |

The Container Apps subnet is delegated to `Microsoft.App/environments`. Private endpoints use a separate subnet from the Container Apps infrastructure.

## Sensitive Backend Paths

| Service | Private Link subresource | Private DNS zone | Public network access |
|---|---|---|---|
| Blob Storage | `blob` | `privatelink.blob.core.windows.net` | Disabled |
| Service Bus | `namespace` | `privatelink.servicebus.windows.net` | Disabled |
| Key Vault | `vault` | `privatelink.vaultcore.azure.net` | Disabled |
| Document Intelligence | `account` | `privatelink.cognitiveservices.azure.com` | Disabled |
| PostgreSQL Flexible Server | `postgresqlServer` | `privatelink.postgres.database.azure.com` | Disabled |

Each private DNS zone is linked to the IntelliDocs VNet. Each private endpoint is associated with its corresponding private DNS zone.

## Service Bus Tier

Service Bus is configured as Premium with capacity `1` to support the private endpoint architecture.

## Application Edge Boundary

P10 does not make the reviewer-facing application private-only.

The Review Portal and API retain external Container Apps ingress as the authenticated application edge. Microsoft Entra authentication and authorization continue to provide the application identity controls introduced in P9.

The five backend dependencies in the sensitive-path matrix are the paths hardened by P10.

A future production deployment may place Front Door/WAF and API Management in front of this edge. That production edge is outside the P10 scope.

## Identity and Secret Boundary

Private networking complements the P9 identity design.

Application workloads continue to use managed identities and RBAC for supported Azure service access. Key Vault remains the secret boundary for configuration that cannot use token-based authentication directly. P10 introduces no application client secret.

## Terraform Evidence

P10 adds or changes Terraform configuration for:

- one IntelliDocs virtual network;
- dedicated Container Apps and private-endpoint subnets;
- five Azure Private DNS zones and VNet links;
- five private endpoints;
- Container Apps environment VNet integration;
- Service Bus Premium;
- public network restrictions for sensitive PaaS dependencies;
- private-network Terraform outputs.

Static module validation passes `terraform fmt -check -recursive` and `terraform validate`.

## Deployment Validation Boundary

The P10 Terraform configuration has been statically validated, but this evidence does not claim a successful P10 `terraform apply`.

Live deployment validation remains necessary to prove private endpoint provisioning, private DNS resolution, application connectivity after public access is disabled, reviewer-facing ingress, and managed-identity authorization across the private paths.

Service Bus Premium can materially change Azure cost relative to Standard. A live apply should therefore be intentional rather than performed solely for static portfolio validation.

## P10 Exit Criterion

**Sensitive paths documented: PASS.**

The Terraform design, network diagram, DNS mapping, and sensitive-path matrix document the intended private-reference boundary while explicitly distinguishing static validation from live Azure deployment evidence.
