from __future__ import annotations

from .models import (
    EvaluationDocument,
    ExtractionMetrics,
    FieldEvaluation,
)
from .normalization import normalize_field_value


def evaluate_extraction(
    documents: list[EvaluationDocument],
) -> ExtractionMetrics:
    field_results: list[FieldEvaluation] = []

    expected_field_count = 0
    predicted_field_count = 0
    matched_field_count = 0
    raw_exact_match_count = 0
    normalized_exact_match_count = 0
    missing_field_count = 0
    unexpected_field_count = 0

    for document in documents:
        expected_by_name = {
            field.name: field
            for field in document.expected_fields
        }

        predicted_by_name = {
            field.name: field
            for field in document.predicted_fields
        }

        expected_field_count += len(expected_by_name)
        predicted_field_count += len(predicted_by_name)

        for field_name, expected in expected_by_name.items():
            predicted = predicted_by_name.get(field_name)

            if predicted is None:
                missing_field_count += 1
                field_results.append(
                    FieldEvaluation(
                        document_id=document.document_id,
                        field_name=field_name,
                        expected_value=expected.value,
                        predicted_value=None,
                        raw_exact_match=False,
                        normalized_exact_match=False,
                        confidence=None,
                        required=expected.required,
                    )
                )
                continue

            matched_field_count += 1

            raw_exact = expected.value == predicted.value

            normalized_exact = (
                normalize_field_value(field_name, expected.value)
                == normalize_field_value(field_name, predicted.value)
            )

            if raw_exact:
                raw_exact_match_count += 1

            if normalized_exact:
                normalized_exact_match_count += 1

            field_results.append(
                FieldEvaluation(
                    document_id=document.document_id,
                    field_name=field_name,
                    expected_value=expected.value,
                    predicted_value=predicted.value,
                    raw_exact_match=raw_exact,
                    normalized_exact_match=normalized_exact,
                    confidence=predicted.confidence,
                    required=expected.required,
                )
            )

        unexpected_field_count += len(
            set(predicted_by_name) - set(expected_by_name)
        )

    raw_rate = (
        raw_exact_match_count / expected_field_count
        if expected_field_count
        else 0.0
    )

    normalized_rate = (
        normalized_exact_match_count / expected_field_count
        if expected_field_count
        else 0.0
    )

    coverage = (
        matched_field_count / expected_field_count
        if expected_field_count
        else 0.0
    )

    return ExtractionMetrics(
        expected_field_count=expected_field_count,
        predicted_field_count=predicted_field_count,
        matched_field_count=matched_field_count,
        raw_exact_match_count=raw_exact_match_count,
        normalized_exact_match_count=normalized_exact_match_count,
        missing_field_count=missing_field_count,
        unexpected_field_count=unexpected_field_count,
        raw_exact_match_rate=raw_rate,
        normalized_exact_match_rate=normalized_rate,
        field_coverage=coverage,
        fields=tuple(field_results),
    )
