# C4 Container Diagram

```mermaid
flowchart TB
    User[External User / System]
    Reviewer[Reviewer]

    FrontDoor[Azure Front Door / WAF]
    APIM[API Management]
    API[ASP.NET Core API]
    Portal[Review Portal]

    Blob[Blob Storage]
    EventGrid[Event Grid]
    Bus[Service Bus]

    Worker[.NET Processing Worker]
    DocAI[Azure AI Document Intelligence]

    DB[(Azure Database for PostgreSQL)]
    KeyVault[Key Vault]
    Monitor[Application Insights / Azure Monitor]

    Downstream[Downstream API / Webhook]

    User --> FrontDoor
    FrontDoor --> APIM
    APIM --> API

    Reviewer --> Portal

    API --> Blob
    API --> DB

    Blob --> EventGrid
    EventGrid --> Bus

    Bus --> Worker
    Worker --> DocAI
    Worker --> DB

    Portal --> DB
    Worker --> Downstream

    API --> KeyVault
    Worker --> KeyVault

    API --> Monitor
    Worker --> Monitor
    Portal --> Monitor
