# Document Processing Sequence

```mermaid
sequenceDiagram
    actor Client
    participant API
    participant Blob
    participant DB as PostgreSQL
    participant EG as Event Grid
    participant SB as Service Bus
    participant Worker
    participant AI as Document Intelligence
    participant Reviewer
    participant Downstream

    Client->>API: Upload document
    API->>API: Calculate SHA-256
    API->>DB: Check duplicate hash
    API->>Blob: Store document
    API->>DB: Create DocumentJob
    API-->>Client: documentId

    Blob-->>EG: Blob-created event
    EG->>SB: Create processing work item
    SB->>Worker: Deliver work item

    Worker->>DB: Claim processing stage idempotently
    Worker->>AI: Analyze document
    AI-->>Worker: Classification + extraction
    Worker->>DB: Store extraction + provenance
    Worker->>Worker: Apply confidence + business rules

    alt Auto-approved
        Worker->>DB: Mark Approved
    else Review required
        Worker->>DB: Mark NeedsReview
        Reviewer->>DB: Accept / edit / reject / reclassify
    end

    opt Approved
        Worker->>Downstream: Publish approved data
        Worker->>DB: Mark Completed
    end
