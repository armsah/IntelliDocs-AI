# P5 Service Bus Failure and Re-drive Evidence

## Purpose

P5 verifies that IntelliDocs AI can recover poison document-processing work without losing the authoritative document state.

The demonstrated flow is:

Queued -> Processing -> retry -> DeadLettered -> re-drive -> Queued -> Processing -> Extracted

Azure Service Bus owns durable work delivery, retry, dead-letter, and re-drive semantics. PostgreSQL remains the authoritative document-job state store.

## Test document

A synthetic invoice committed under `samples/synthetic/` was used.

The live recovery demonstration used a dedicated test tenant. No production or personal document content was used.

## Retry and dead-letter demonstration

A deterministic worker failure was enabled for one test document.

The worker received the initial `layout-extraction` message with broker generation `r0`.

Observed behavior:

- delivery 1 failed and the message was abandoned
- delivery 2 failed and the message was abandoned
- delivery 3 failed and the message was abandoned
- delivery 4 failed and the message was abandoned
- delivery 5 failed
- PostgreSQL transitioned the document to `DeadLettered`
- the worker explicitly dead-lettered the Service Bus message
- dead-letter reason: `ProcessingFailed`

The queue is configured with:

- PeekLock processing
- explicit message settlement
- maximum delivery count of 5
- dead-lettering on message expiration
- duplicate detection
- a 10-minute duplicate-detection history window

This demonstrates bounded retry behavior for poison work.

## Idempotency

Application processing uses the stable key:

`documentId + processingStage`

The Service Bus broker MessageId additionally contains the re-drive generation:

`{documentId}:{processingStage}:r{redriveCount}`

For example:

`{documentId}:layout-extraction:r0`

becomes:

`{documentId}:layout-extraction:r1`

after the first re-drive.

This preserves application-level idempotency while allowing an intentional re-drive to bypass Service Bus duplicate suppression for the earlier generation.

## Controlled re-drive

After the poison message entered the DLQ, deterministic failure injection was disabled.

The re-drive utility:

1. loaded the authoritative document from PostgreSQL
2. located the matching Service Bus DLQ message
3. validated the document and tenant
4. transitioned PostgreSQL from `DeadLettered` to `Queued`
5. incremented the re-drive generation from `r0` to `r1`
6. published the replacement message
7. completed the original DLQ message only after successful publication

Observed result:

- PostgreSQL: `DeadLettered -> Queued`
- replacement generation: `r1`
- original DLQ message completed
- re-drive completed successfully

## Recovery

The worker was restarted with deterministic failure injection disabled.

It consumed the `r1` replacement message and:

1. transitioned the document to `Processing`
2. opened the durable Blob document
3. invoked Azure AI Document Intelligence using `prebuilt-layout`
4. persisted the normalized analysis result
5. persisted AI model metadata
6. transitioned the document to `Extracted`
7. explicitly completed the Service Bus message

The final worker log confirmed successful settlement of the `r1` message.

## Exit criterion

P5 therefore demonstrates:

> Poison work is recoverable.

The failure path is bounded, observable, dead-lettered, and manually re-drivable without treating Service Bus queue presence as the authoritative business state.

## Security note

No Service Bus connection strings, Document Intelligence keys, PostgreSQL passwords, or other credentials are included in this evidence.

P5 uses local development credentials supplied through environment variables. Managed identity, Key Vault, and least-privilege Azure RBAC are intentionally deferred to P9.
