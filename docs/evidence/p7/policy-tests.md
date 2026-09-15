# P7 Confidence Policy and Business Validation Evidence

## Objective

P7 implements deterministic confidence-based routing and business validation after document classification and extraction.

Exit criterion: deterministic routing backed by automated policy tests.

## Confidence Policy

The policy preserves the P0 thresholds:

| Policy confidence | Routing |
| --- | --- |
| >= 0.90 | Auto-approve only when the document type supports structured validation and no blocking business-rule issue exists |
| >= 0.70 and < 0.90 | NeedsReview |
| < 0.70 | NeedsReview with LOW_CONFIDENCE warning |

Policy confidence is the minimum of:

- classifier confidence;
- confidence values for present mandatory extracted fields.

Missing mandatory fields are represented as explicit business-rule failures rather than artificially forcing confidence to zero.

The 0.90 auto-approval threshold was not weakened to accommodate the P6 classifier confidence distribution.

## Structured Document Rules

### Invoice

Mandatory fields:

- invoiceNumber
- invoiceDate
- supplierName
- customerName
- currency
- totalAmount

### Purchase Order

Mandatory fields:

- purchaseOrderNumber
- orderDate
- buyerName
- supplierName
- currency
- totalAmount

### Delivery Note

Mandatory fields:

- deliveryNoteNumber
- deliveryDate
- supplierName
- customerName

Contract, form, unknown, and unsupported document types require manual review and are not eligible for structured auto-approval in P7.

## Normalization

Normalization is deterministic and does not infer missing values.

Implemented normalization includes:

- German and English date representations to ISO yyyy-MM-dd;
- German and English decimal separators to invariant decimal representation;
- currency symbols/names to ISO-style codes where explicitly known;
- identifier upper-casing;
- whitespace normalization.

## Business Validation

Blocking validation includes:

- missing mandatory fields;
- invalid dates;
- invalid total amounts;
- invalid currency codes;
- negative total amounts.

A negative total amount is a Critical issue.

Unsupported or non-structured document types receive MANUAL_REVIEW_DOCUMENT_TYPE.

## Worker Routing

The Service Bus worker performs classification and extraction, then applies the deterministic policy before completing the message.

Successful state paths are:

    Processing -> Extracted -> Validating -> Approved

or:

    Processing -> Extracted -> Validating -> NeedsReview

The classification, extraction result, normalized validation result, routing decision, policy confidence, and validation issues are persisted together in the document analysis JSON envelope.

## Retry and DLQ Safety

P7 preserves the P5 at-least-once processing behavior.

Before failure handling, the worker reloads the DocumentJob from PostgreSQL. This prevents unsaved in-memory validation transitions from being mistaken for durable state.

Therefore:

- if persistence failed before commit, the durable Processing state remains eligible for retry or DeadLettered at the maximum delivery count;
- if persistence succeeded but Service Bus completion failed, the durable Approved or NeedsReview state is reloaded and is not falsely changed to DeadLettered;
- redelivery of a durably completed validation state is handled idempotently.

## Automated Policy Coverage

Policy tests cover:

- 0.900 auto-approval boundary;
- 0.899 review boundary;
- 0.700 review boundary;
- 0.699 low-confidence boundary;
- weakest mandatory-field confidence controlling policy confidence;
- missing mandatory fields;
- negative total amount critical validation;
- contract manual review;
- form manual review;
- unknown/manual document type;
- valid purchase-order auto-approval;
- valid delivery-note auto-approval;
- deterministic date, amount, currency, whitespace, and identifier normalization;
- Processing -> Extracted -> Validating -> Approved routing;
- Processing -> Extracted -> Validating -> NeedsReview routing;
- Processing -> DeadLettered failure path.

## Verification

Focused P7 validation tests were executed with:

    dotnet test tests\IntelliDocs.UnitTests\IntelliDocs.UnitTests.csproj --filter "FullyQualifiedName~IntelliDocs.UnitTests.Validation"

The complete .NET regression suite was executed with:

    dotnet test IntelliDocs.slnx

Final complete-suite result:

    total: 55
    failed: 0
    succeeded: 55
    skipped: 0

P7 exit criterion: PASS.
