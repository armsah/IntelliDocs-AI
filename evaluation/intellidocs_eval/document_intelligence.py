from __future__ import annotations

import json
from pathlib import Path

from .models import PredictedField


INVOICE_FIELD_MAP = {
    "InvoiceId": "invoiceNumber",
    "InvoiceDate": "invoiceDate",
    "DueDate": "dueDate",
    "VendorName": "supplierName",
    "VendorTaxId": "supplierTaxId",
    "CustomerName": "customerName",
    "CustomerTaxId": "customerTaxId",
    "PurchaseOrder": "purchaseOrderNumber",
    "SubTotal": "subtotal",
    "TotalTax": "taxAmount",
    "InvoiceTotal": "totalAmount",
    "AmountDue": "amountDue",
}


def load_document_intelligence_fields(
    path: str | Path,
) -> tuple[PredictedField, ...]:
    source = Path(path)

    with source.open("r", encoding="utf-8-sig") as stream:
        payload = json.load(stream)

    model_id = _first_string(
        payload,
        "ModelId",
        "modelId",
    )

    fields = _find_fields(payload)

    result: list[PredictedField] = []

    for item in fields:
        if not isinstance(item, dict):
            continue

        source_name = _first_string(
            item,
            "Name",
            "name",
            "FieldName",
            "fieldName",
        )

        if not source_name:
            continue

        value = _field_value(item)

        if value is None or not value.strip():
            continue

        confidence = _confidence(item)

        name = _map_field_name(
            model_id,
            source_name,
        )

        result.append(
            PredictedField(
                name=name,
                value=value,
                confidence=confidence,
            )
        )

    return tuple(result)


def load_model_id(
    path: str | Path,
) -> str | None:
    source = Path(path)

    with source.open("r", encoding="utf-8-sig") as stream:
        payload = json.load(stream)

    if not isinstance(payload, dict):
        return None

    return _first_string(
        payload,
        "ModelId",
        "modelId",
    )


def _map_field_name(
    model_id: str | None,
    source_name: str,
) -> str:
    if model_id == "prebuilt-invoice":
        return INVOICE_FIELD_MAP.get(
            source_name,
            source_name,
        )

    return source_name


def _find_fields(payload: object) -> list[object]:
    if not isinstance(payload, dict):
        return []

    for key in ("Fields", "fields"):
        direct = payload.get(key)

        if isinstance(direct, list):
            return direct

    for documents_key in ("Documents", "documents"):
        documents = payload.get(documents_key)

        if not isinstance(documents, list):
            continue

        result: list[object] = []

        for document in documents:
            if not isinstance(document, dict):
                continue

            for fields_key in ("Fields", "fields"):
                fields = document.get(fields_key)

                if isinstance(fields, list):
                    result.extend(fields)

        return result

    return []


def _field_value(item: dict[str, object]) -> str | None:
    for key in (
        "NormalizedValue",
        "normalizedValue",
        "Content",
        "content",
        "RawValue",
        "rawValue",
        "Value",
        "value",
    ):
        value = item.get(key)

        if value is None:
            continue

        if isinstance(value, str):
            return value

        if isinstance(value, (int, float)):
            return str(value)

    return None


def _confidence(
    item: dict[str, object],
) -> float | None:
    value = item.get("Confidence")

    if value is None:
        value = item.get("confidence")

    if isinstance(value, bool):
        return None

    if isinstance(value, (int, float)):
        return float(value)

    return None


def _first_string(
    item: dict[str, object],
    *keys: str,
) -> str | None:
    for key in keys:
        value = item.get(key)

        if isinstance(value, str) and value.strip():
            return value

    return None
