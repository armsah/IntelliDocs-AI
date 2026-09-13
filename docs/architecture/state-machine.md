# Document Job State Machine

The PostgreSQL document job record is the source of truth for processing state.

Queue presence must never be interpreted as document state.

```mermaid
stateDiagram-v2
    [*] --> Submitted

    Submitted --> Stored
    Stored --> Queued
    Queued --> Processing

    Processing --> Extracted
    Processing --> Failed
    Processing --> DeadLettered

    Extracted --> Validating

    Validating --> Approved
    Validating --> NeedsReview

    NeedsReview --> InReview
    InReview --> Approved
    InReview --> Rejected

    Approved --> Publishing
    Publishing --> Completed
    Publishing --> Failed

    DeadLettered --> Queued: Re-drive
    Failed --> Queued: Retry / reprocess

    Completed --> [*]
    Rejected --> [*]
