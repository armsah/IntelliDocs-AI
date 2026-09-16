# P12 Failure and Recovery Evidence

## Objective

Confirm that IntelliDocs has deterministic behavior for transient processing
failure, poison work, dead-lettering, and controlled recovery.

P12 reuses the live Service Bus recovery demonstration captured in P5 and
revalidates the corresponding implementation and automated regression
coverage. A second destructive Azure failure exercise was intentionally not
performed.

## Failure model

Service Bus owns durable work-delivery semantics while PostgreSQL remains the
authoritative document business-state store.

The worker supports:

- PeekLock processing
- explicit completion
- abandon for retry
- bounded broker delivery
- explicit dead-lettering
- persisted `DeadLettered` document state
- controlled re-drive from the DLQ

## Automated coverage

The current test suite includes coverage for:

- transient document processing failure
- retry state transitions
- `DeadLettered -> Queued` re-drive
- validation/business-routing paths that can dead-letter
- re-drive generation metadata

These tests are part of the P12 full .NET regression gate.

## Existing live Service Bus evidence

The P5 failure demonstration exercised a synthetic document through:

`Queued -> Processing -> retry -> DeadLettered -> re-drive -> Queued -> Processing -> Extracted`

Observed P5 behavior:

1. deliveries 1 through 4 failed and were abandoned
2. delivery 5 failed
3. PostgreSQL transitioned the document to `DeadLettered`
4. the worker explicitly dead-lettered the broker message
5. the failure injection was removed
6. the re-drive utility changed the generation from `r0` to `r1`
7. PostgreSQL transitioned `DeadLettered -> Queued`
8. a replacement Service Bus message was published
9. the original DLQ message was completed
10. the worker consumed the replacement and recovered the document to
    `Extracted`

The configured maximum delivery count was five.

## Re-drive safety

`tools/IntelliDocs.ServiceBus.Redrive` verifies:

- requested document exists in PostgreSQL
- document state permits re-drive
- a matching DLQ message exists
- message document ID matches the requested document
- message tenant matches the persisted tenant
- re-drive generation is incremented
- replacement publication succeeds before the original DLQ message is
  completed

If replacement publication fails, the original DLQ message is deliberately
left uncompleted.

The generation-specific broker MessageId supports safe intentional re-drive
while retaining the stable application idempotency key based on document and
processing stage.

## P12 decision

No additional destructive live-Azure failure was required for portfolio
polish. The existing P5 live evidence demonstrates the broker failure path,
and P12's 65-test regression gate revalidates the deterministic state and
routing behavior.

## Exit

PASS for failure/recovery evidence.

Poison work is bounded, observable, dead-lettered, and recoverable through a
controlled re-drive mechanism without making Service Bus the authoritative
business-state store.