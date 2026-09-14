# P6 Document AI Model Selection Decision

## Scope

P6 evaluates how IntelliDocs AI supports the five P0 target document types:

- invoice
- purchase order
- delivery note
- contract
- form

Unknown or unsupported documents remain an application-level manual-classification path.

## Evidence boundary

The documents in `samples/synthetic/p6` are fictional portfolio fixtures.

They are suitable for:

- pipeline verification;
- Azure integration verification;
- model-routing verification;
- evaluation-tool verification;
- demonstrating capability gaps.

They are not representative production-quality evaluation data and must not be used to claim production precision, recall, or accuracy.

Production model-quality evaluation requires a separate representative, sanitized, legally usable labeled dataset.

## Invoice

Current model:

`prebuilt-invoice`

Observed synthetic baseline:

- OCR/extraction completed successfully;
- 10 of 11 expected labeled fields were returned;
- field coverage: 0.9091;
- normalized exact-match rate: 0.9091;
- missing target field: `currency`;
- four additional address fields were returned by Azure;
- `supplierTaxId` confidence was approximately 0.747.

Decision:

Retain `prebuilt-invoice` for the P6 architecture.

The synthetic baseline does not justify replacing it with custom extraction. The missing currency representation should be handled separately through typed-value mapping, query-field/schema extension, normalization, or later validation work.

## Purchase order

Plain `prebuilt-layout` baseline:

- OCR content recovered successfully;
- purchase-order identifier was recovered in document text;
- semantic `Fields` collection contained zero fields;
- structured field coverage was 0.0000.

P0 requires these structured mandatory fields:

- `purchaseOrderNumber`
- `orderDate`
- `buyerName`
- `supplierName`
- `currency`
- `totalAmount`

Query-field evaluation using `prebuilt-layout` returned:

- 6 of 6 expected fields;
- field coverage: 1.0000;
- raw exact-match rate: 1.0000;
- normalized exact-match rate: 1.0000;
- missing fields: 0;
- unexpected fields: 0;
- average confidence for each evaluated field: approximately 0.9950;
- field calibration ECE: 0.0050.

Decision:

Use `prebuilt-layout` with schema-directed query fields for purchase-order extraction.

The measured synthetic result does not justify training a custom purchase-order extraction model for P6.

`totalAmount` is returned as document text including the currency token. Typed amount/currency normalization and business validation remain downstream concerns.

Evidence:

- `docs/evidence/p6/query-purchase-order.json`
- `docs/evidence/p6/purchase-order-query-report.json`
- `docs/evidence/p6/purchase-order-query-report.md`

## Delivery note

Plain `prebuilt-layout` baseline:

- OCR content recovered successfully;
- delivery-note identifier was recovered in document text;
- semantic `Fields` collection contained zero fields;
- structured field coverage was 0.0000.

P0 requires these structured mandatory fields:

- `deliveryNoteNumber`
- `deliveryDate`
- `supplierName`
- `customerName`

Query-field evaluation using `prebuilt-layout` returned:

- 4 of 4 expected fields;
- field coverage: 1.0000;
- raw exact-match rate: 1.0000;
- normalized exact-match rate: 1.0000;
- missing fields: 0;
- unexpected fields: 0;
- average confidence for each evaluated field: approximately 0.9950;
- field calibration ECE: 0.0050.

Decision:

Use `prebuilt-layout` with schema-directed query fields for delivery-note extraction.

The measured synthetic result does not justify training a custom delivery-note extraction model for P6.

Evidence:

- `docs/evidence/p6/query-delivery-note.json`
- `docs/evidence/p6/delivery-note-query-report.json`
- `docs/evidence/p6/delivery-note-query-report.md`

## Contract

P0 requires classification plus OCR/layout initially.

Observed plain-layout OCR recovered the contract identifier and document content.

Decision:

Keep `prebuilt-layout` extraction for P6. No custom contract extraction is required for the current P0 scope.

## Form

P0 requires classification plus OCR/layout initially.

Observed plain-layout OCR recovered the form reference and document content.

Decision:

Keep `prebuilt-layout` extraction for P6. No custom form extraction is required for the current P0 scope.

## Classification

The current IntelliDocs processing path does not provide five-type document classification.

Decision:

Add a Document Intelligence custom classifier covering:

- `invoice`
- `purchase_order`
- `delivery_note`
- `contract`
- `form`

Low-confidence, unsupported, or unknown documents must route to manual classification rather than being forced into a known class.

Azure training-data minimums are not treated as model-quality targets. Representative held-out evaluation data remains necessary before production use.

## Custom extraction decision

No custom extraction model is introduced in P6.

The current evidence supports:

- invoice: `prebuilt-invoice`;
- purchase order: `prebuilt-layout` plus query fields;
- delivery note: `prebuilt-layout` plus query fields;
- contract: `prebuilt-layout`;
- form: `prebuilt-layout`.

Custom extraction remains an option only if future representative evaluation demonstrates a material quality gap.

## Target P6 routing

```text
document
   |
   v
custom classifier
   |
   +-- invoice ----------> prebuilt-invoice
   |
   +-- purchase_order ---> prebuilt-layout + query fields
   |
   +-- delivery_note ----> prebuilt-layout + query fields
   |
   +-- contract ---------> prebuilt-layout
   |
   +-- form -------------> prebuilt-layout
   |
   +-- low confidence /
       unsupported ------> manual classification
