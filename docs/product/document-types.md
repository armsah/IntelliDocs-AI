# Document Types and Extraction Fields

## Supported Types

| Document Type | Classification | Structured Extraction | Human Review |
|---|---|---|---|
| Invoice | Yes | Yes | Yes |
| Purchase Order | Yes | Yes | Yes |
| Delivery Note | Yes | Yes | Yes |
| Contract | Yes | OCR/layout initially | Yes |
| Form | Yes | OCR/layout initially | Yes |
| Unknown | Manual | No automatic extraction | Required |

## Invoice

Mandatory candidate fields:

- invoiceNumber
- invoiceDate
- supplierName
- customerName
- currency
- totalAmount

Optional fields:

- supplierTaxId
- subtotal
- taxAmount
- dueDate
- purchaseOrderNumber
- lineItems[]

### Invoice line item

- description
- quantity
- unitPrice
- taxRate
- lineTotal

## Purchase Order

Mandatory candidate fields:

- purchaseOrderNumber
- orderDate
- buyerName
- supplierName
- currency
- totalAmount

Optional fields:

- subtotal
- taxAmount
- deliveryDate
- lineItems[]

### Purchase Order line item

- itemCode
- description
- quantity
- unitPrice
- lineTotal

## Delivery Note

Mandatory candidate fields:

- deliveryNoteNumber
- deliveryDate
- supplierName
- customerName

Optional fields:

- purchaseOrderNumber
- deliveryAddress
- lineItems[]

### Delivery Note line item

- itemCode
- description
- quantity

## Field Provenance

Every extracted field must retain:

- fieldName
- rawValue
- normalizedValue
- confidence
- pageNumber
- boundingRegion
- validationStatus
- reviewerDecision

This provenance allows reviewers to trace an extracted value back to the original page.

## Unknown Documents

Unknown or unsupported document types must not be forced into an extraction schema.

They are routed to manual classification.
