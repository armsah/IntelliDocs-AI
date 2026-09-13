# C4 Context Diagram

```mermaid
flowchart LR
    Submitter[Customer / Supplier]
    Reviewer[Human Reviewer]
    Admin[Administrator]
    Downstream[ERP / Downstream System]

    System[IntelliDocs AI]

    Submitter -->|Uploads business documents| System
    Reviewer -->|Reviews uncertain extractions| System
    Admin -->|Configures policies and monitors processing| System
    System -->|Publishes approved structured data| Downstream
