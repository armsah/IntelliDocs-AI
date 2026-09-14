# IntelliDocs AI Evaluation Report

Documents evaluated: **1**

## Classification

- Accuracy: **not evaluated**
- Correct: **0 / 0**

### Per-class metrics

| Class | Precision | Recall | F1 | Support |
| --- | ---: | ---: | ---: | ---: |

## Extraction

- Field coverage: **0.9091**
- Raw exact-match rate: **0.9091**
- Normalized exact-match rate: **0.9091**
- Missing fields: **1**
- Unexpected fields: **4**

### Per-field metrics

| Field | Expected | Coverage | Normalized accuracy | Avg confidence |
| --- | ---: | ---: | ---: | ---: |
| BillingAddress | 0 | 0.0000 | 0.0000 | - |
| BillingAddressRecipient | 0 | 0.0000 | 0.0000 | - |
| VendorAddress | 0 | 0.0000 | 0.0000 | - |
| VendorAddressRecipient | 0 | 0.0000 | 0.0000 | - |
| currency | 1 | 0.0000 | 0.0000 | - |
| customerName | 1 | 1.0000 | 1.0000 | 0.9280 |
| dueDate | 1 | 1.0000 | 1.0000 | 0.9820 |
| invoiceDate | 1 | 1.0000 | 1.0000 | 0.9830 |
| invoiceNumber | 1 | 1.0000 | 1.0000 | 0.9830 |
| purchaseOrderNumber | 1 | 1.0000 | 1.0000 | 0.9820 |
| subtotal | 1 | 1.0000 | 1.0000 | 0.9390 |
| supplierName | 1 | 1.0000 | 1.0000 | 0.9160 |
| supplierTaxId | 1 | 1.0000 | 1.0000 | 0.7470 |
| taxAmount | 1 | 1.0000 | 1.0000 | 0.9380 |
| totalAmount | 1 | 1.0000 | 1.0000 | 0.9380 |

## Per-document-type summary

| Type | Documents | Classification accuracy | Field coverage | Normalized extraction |
| --- | ---: | ---: | ---: | ---: |
| invoice | 1 | not evaluated | 0.9091 | 0.9091 |

## Calibration

- Classification ECE: **0.0000**
- Field ECE: **0.0664**

## Notes

Metrics describe only the supplied evaluation dataset. Dataset provenance and representativeness must be reviewed before treating results as production model-quality evidence.
