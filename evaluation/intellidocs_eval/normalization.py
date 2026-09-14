from __future__ import annotations

import re
import unicodedata
from decimal import Decimal, InvalidOperation


_WHITESPACE = re.compile(r"\s+")


def normalize_text(value: str | None) -> str:
    if value is None:
        return ""

    value = unicodedata.normalize("NFKC", value)
    value = value.strip()
    value = _WHITESPACE.sub(" ", value)

    return value.casefold()


def normalize_identifier(value: str | None) -> str:
    normalized = normalize_text(value)

    return re.sub(r"[\s\-_/.:]+", "", normalized)


def normalize_number(value: str | None) -> str:
    if value is None:
        return ""

    candidate = unicodedata.normalize("NFKC", value).strip()
    candidate = candidate.replace("\u00a0", "").replace(" ", "")
    candidate = re.sub(r"[€$£]", "", candidate)

    if "," in candidate and "." in candidate:
        if candidate.rfind(",") > candidate.rfind("."):
            candidate = candidate.replace(".", "").replace(",", ".")
        else:
            candidate = candidate.replace(",", "")
    elif "," in candidate:
        candidate = candidate.replace(",", ".")

    try:
        number = Decimal(candidate)
    except InvalidOperation:
        return normalize_text(value)

    normalized = format(number.normalize(), "f")

    if "." in normalized:
        normalized = normalized.rstrip("0").rstrip(".")

    return normalized


def normalize_field_value(field_name: str, value: str | None) -> str:
    field = field_name.casefold()

    identifier_fields = {
        "invoicenumber",
        "purchaseordernumber",
        "deliverynotenumber",
        "suppliertaxid",
    }

    numeric_fields = {
        "subtotal",
        "taxamount",
        "totalamount",
        "quantity",
        "unitprice",
        "lineamount",
    }

    compact_name = re.sub(r"[^a-z0-9]", "", field)

    if compact_name in identifier_fields:
        return normalize_identifier(value)

    if compact_name in numeric_fields:
        return normalize_number(value)

    return normalize_text(value)
