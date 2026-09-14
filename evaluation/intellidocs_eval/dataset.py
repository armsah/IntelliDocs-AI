from __future__ import annotations

import json
from pathlib import Path

from .models import (
    EvaluationDocument,
    ExpectedField,
    PredictedField,
)


def load_evaluation_dataset(path: str | Path) -> list[EvaluationDocument]:
    dataset_path = Path(path)

    with dataset_path.open("r", encoding="utf-8-sig") as stream:
        payload = json.load(stream)

    if not isinstance(payload, dict):
        raise ValueError("Evaluation dataset root must be a JSON object.")

    documents_payload = payload.get("documents")

    if not isinstance(documents_payload, list):
        raise ValueError(
            "Evaluation dataset must contain a 'documents' array."
        )

    documents: list[EvaluationDocument] = []

    for item in documents_payload:
        if not isinstance(item, dict):
            raise ValueError("Each document must be a JSON object.")

        document_id = _required_string(item, "documentId")
        expected_type = _required_string(item, "expectedType")

        predicted_type = _optional_string(
            item.get("predictedType")
        )

        classification_confidence = _optional_confidence(
            item.get("classificationConfidence")
        )

        expected_fields = tuple(
            _load_expected_field(field)
            for field in item.get("expectedFields", [])
        )

        predicted_fields = tuple(
            _load_predicted_field(field)
            for field in item.get("predictedFields", [])
        )

        documents.append(
            EvaluationDocument(
                document_id=document_id,
                expected_type=expected_type,
                predicted_type=predicted_type,
                classification_confidence=classification_confidence,
                expected_fields=expected_fields,
                predicted_fields=predicted_fields,
            )
        )

    return documents


def _load_expected_field(item: object) -> ExpectedField:
    if not isinstance(item, dict):
        raise ValueError("Expected field must be a JSON object.")

    return ExpectedField(
        name=_required_string(item, "name"),
        value=_required_string(item, "value"),
        required=bool(item.get("required", False)),
    )


def _load_predicted_field(item: object) -> PredictedField:
    if not isinstance(item, dict):
        raise ValueError("Predicted field must be a JSON object.")

    return PredictedField(
        name=_required_string(item, "name"),
        value=_required_string(item, "value"),
        confidence=_optional_confidence(item.get("confidence")),
    )


def _required_string(
    item: dict[str, object],
    name: str,
) -> str:
    value = item.get(name)

    if not isinstance(value, str) or not value.strip():
        raise ValueError(
            f"'{name}' must be a non-empty string."
        )

    return value


def _optional_string(value: object) -> str | None:
    if value is None:
        return None

    if not isinstance(value, str):
        raise ValueError("Optional string value must be a string.")

    return value


def _optional_confidence(value: object) -> float | None:
    if value is None:
        return None

    if isinstance(value, bool) or not isinstance(
        value,
        (int, float),
    ):
        raise ValueError(
            "Confidence must be a number between 0 and 1."
        )

    result = float(value)

    if result < 0.0 or result > 1.0:
        raise ValueError(
            "Confidence must be between 0 and 1."
        )

    return result
