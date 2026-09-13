# Confidence and Human Review Policy

AI confidence is an input to the routing decision, not the final authority.

Thresholds are configuration values per document type and must later be validated using a labeled evaluation dataset.

## Default Thresholds

| Condition | Route |
|---|---|
| All mandatory fields >= 0.90 and business rules pass | Auto-approve |
| Any mandatory field from 0.70 through 0.89 | Human review |
| Any mandatory field < 0.70 | Human review with low-confidence warning |
| Critical business-rule conflict | Human review regardless of confidence |
| Unknown document type | Manual classification |

## Default Configuration Example

```yaml
documentTypes:
  invoice:
    mandatoryFieldAutoApproveThreshold: 0.90
    mandatoryFieldReviewThreshold: 0.70

  purchaseOrder:
    mandatoryFieldAutoApproveThreshold: 0.90
    mandatoryFieldReviewThreshold: 0.70

  deliveryNote:
    mandatoryFieldAutoApproveThreshold: 0.90
    mandatoryFieldReviewThreshold: 0.70
