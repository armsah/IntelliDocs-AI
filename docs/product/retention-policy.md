# Data Retention Policy

This document defines the initial development and portfolio retention policy.

Production retention must ultimately be aligned with legal, contractual, privacy, and enterprise records-management requirements.

## Data Categories

### Source documents

Original uploaded documents are stored in Azure Blob Storage.

Initial development retention target:

- 90 days for non-production test documents.

### Extraction results

Structured extraction versions are retained for:

- traceability;
- review history;
- model comparison;
- reprocessing.

Development target:

- 180 days.

### Audit events

Audit records capture significant state changes and reviewer actions.

Target:

- minimum 365 days for the portfolio environment.

Production policy should be configurable according to enterprise compliance requirements.

### Operational telemetry

Application logs and traces should not contain document payloads or sensitive extracted values unless explicitly approved.

Development target:

- 30 days for high-volume operational telemetry.

## Deletion

Deletion must account for:

- Blob Storage objects;
- database records;
- derived extraction versions;
- generated previews;
- search/index artifacts if later introduced.

## Security

Retention does not override least-privilege requirements.

Sensitive document content must remain encrypted in transit and at rest.
