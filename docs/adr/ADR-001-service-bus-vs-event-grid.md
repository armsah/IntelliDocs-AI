# ADR-001: Service Bus vs Event Grid Responsibility

- Status: Accepted
- Phase: P0

## Context

Blob Storage needs to notify the processing platform when new documents arrive.

Azure Event Grid provides event notification, while Azure Service Bus provides durable messaging and work-item processing capabilities.

Using Event Grid alone would make retry control, dead-letter handling, concurrency management, and operational re-drive more difficult.

## Decision

Use Event Grid for resource-event notification.

Use Service Bus for durable document-processing work items.

### Event Grid responsibility

- detect Blob Storage changes;
- emit events when relevant documents arrive;
- decouple Blob Storage from processing infrastructure.

### Service Bus responsibility

- durable work delivery;
- controlled concurrency;
- retry behavior;
- duplicate-safe consumer processing;
- dead-lettering;
- operational re-drive.

## Consequences

Positive:

- explicit processing semantics;
- recoverable poison messages;
- improved observability;
- controlled worker throughput;
- easier operational re-drive.

Trade-offs:

- additional Azure service;
- additional Terraform configuration;
- more operational components.

## Decision Rule

Event Grid signals that something happened.

Service Bus owns the work required because of that event.
