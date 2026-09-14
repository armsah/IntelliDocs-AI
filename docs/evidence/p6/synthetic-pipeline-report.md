# IntelliDocs AI Evaluation Report

Documents evaluated: **5**

## Classification

- Accuracy: **0.8000**
- Correct: **4 / 5**

### Per-class metrics

| Class | Precision | Recall | F1 | Support |
| --- | ---: | ---: | ---: | ---: |
| contract | 0.5000 | 1.0000 | 0.6667 | 1 |
| delivery_note | 1.0000 | 1.0000 | 1.0000 | 1 |
| form | 0.0000 | 0.0000 | 0.0000 | 1 |
| invoice | 1.0000 | 1.0000 | 1.0000 | 1 |
| purchase_order | 1.0000 | 1.0000 | 1.0000 | 1 |

## Extraction

- Field coverage: **1.0000**
- Raw exact-match rate: **0.2500**
- Normalized exact-match rate: **1.0000**
- Missing fields: **0**
- Unexpected fields: **0**

### Per-field metrics

| Field | Expected | Coverage | Normalized accuracy | Avg confidence |
| --- | ---: | ---: | ---: | ---: |
| deliveryNoteNumber | 1 | 1.0000 | 1.0000 | 0.9000 |
| invoiceNumber | 1 | 1.0000 | 1.0000 | 0.9700 |
| purchaseOrderNumber | 1 | 1.0000 | 1.0000 | 0.9300 |
| totalAmount | 1 | 1.0000 | 1.0000 | 0.9500 |

## Per-document-type summary

| Type | Documents | Classification accuracy | Field coverage | Normalized extraction |
| --- | ---: | ---: | ---: | ---: |
| contract | 1 | 1.0000 | 0.0000 | 0.0000 |
| delivery_note | 1 | 1.0000 | 1.0000 | 1.0000 |
| form | 1 | 0.0000 | 0.0000 | 0.0000 |
| invoice | 1 | 1.0000 | 1.0000 | 1.0000 |
| purchase_order | 1 | 1.0000 | 1.0000 | 1.0000 |

## Calibration

- Classification ECE: **0.2160**
- Field ECE: **0.0625**

## Notes

Synthetic datasets validate the evaluation pipeline and must not be represented as production model-quality evidence.
