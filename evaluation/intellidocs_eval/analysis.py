from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass

from .models import EvaluationDocument, ExtractionMetrics


@dataclass(frozen=True)
class FieldSummary:
    field_name: str
    expected_count: int
    predicted_count: int
    matched_count: int
    normalized_correct_count: int
    missing_count: int
    normalized_accuracy: float
    coverage: float
    average_confidence: float | None


@dataclass(frozen=True)
class TypeSummary:
    document_type: str
    document_count: int
    classification_evaluated_count: int
    classification_correct: int
    classification_accuracy: float | None
    expected_field_count: int
    normalized_correct_count: int
    field_coverage: float
    normalized_exact_match_rate: float


def build_field_summaries(
    documents: list[EvaluationDocument],
    extraction: ExtractionMetrics,
) -> dict[str, FieldSummary]:
    expected_count: dict[str, int] = defaultdict(int)
    predicted_count: dict[str, int] = defaultdict(int)
    matched_count: dict[str, int] = defaultdict(int)
    correct_count: dict[str, int] = defaultdict(int)
    missing_count: dict[str, int] = defaultdict(int)
    confidences: dict[str, list[float]] = defaultdict(list)

    for document in documents:
        for field in document.expected_fields:
            expected_count[field.name] += 1

        for field in document.predicted_fields:
            predicted_count[field.name] += 1

    for field in extraction.fields:
        if field.predicted_value is None:
            missing_count[field.field_name] += 1
            continue

        matched_count[field.field_name] += 1

        if field.normalized_exact_match:
            correct_count[field.field_name] += 1

        if field.confidence is not None:
            confidences[field.field_name].append(
                field.confidence
            )

    names = sorted(set(expected_count) | set(predicted_count))

    summaries: dict[str, FieldSummary] = {}

    for name in names:
        expected = expected_count[name]
        matched = matched_count[name]
        correct = correct_count[name]

        coverage = matched / expected if expected else 0.0
        normalized_accuracy = (
            correct / expected if expected else 0.0
        )

        values = confidences[name]

        average_confidence = (
            sum(values) / len(values)
            if values
            else None
        )

        summaries[name] = FieldSummary(
            field_name=name,
            expected_count=expected,
            predicted_count=predicted_count[name],
            matched_count=matched,
            normalized_correct_count=correct,
            missing_count=missing_count[name],
            normalized_accuracy=normalized_accuracy,
            coverage=coverage,
            average_confidence=average_confidence,
        )

    return summaries


def build_type_summaries(
    documents: list[EvaluationDocument],
    extraction: ExtractionMetrics,
) -> dict[str, TypeSummary]:
    by_type: dict[str, list[EvaluationDocument]] = defaultdict(list)

    for document in documents:
        by_type[document.expected_type].append(document)

    field_results_by_document = defaultdict(list)

    for field in extraction.fields:
        field_results_by_document[field.document_id].append(field)

    summaries: dict[str, TypeSummary] = {}

    for document_type in sorted(by_type):
        typed_documents = by_type[document_type]

        classification_documents = [
            document
            for document in typed_documents
            if document.predicted_type is not None
        ]

        classification_correct = sum(
            1
            for document in classification_documents
            if document.expected_type == document.predicted_type
        )

        classification_accuracy = (
            classification_correct / len(classification_documents)
            if classification_documents
            else None
        )

        expected_fields = sum(
            len(document.expected_fields)
            for document in typed_documents
        )

        matched_fields = 0
        normalized_correct = 0

        for document in typed_documents:
            for result in field_results_by_document[
                document.document_id
            ]:
                if result.predicted_value is not None:
                    matched_fields += 1

                if result.normalized_exact_match:
                    normalized_correct += 1

        coverage = (
            matched_fields / expected_fields
            if expected_fields
            else 0.0
        )

        normalized_rate = (
            normalized_correct / expected_fields
            if expected_fields
            else 0.0
        )

        summaries[document_type] = TypeSummary(
            document_type=document_type,
            document_count=len(typed_documents),
            classification_evaluated_count=len(
                classification_documents
            ),
            classification_correct=classification_correct,
            classification_accuracy=classification_accuracy,
            expected_field_count=expected_fields,
            normalized_correct_count=normalized_correct,
            field_coverage=coverage,
            normalized_exact_match_rate=normalized_rate,
        )

    return summaries
