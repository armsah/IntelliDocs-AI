from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(frozen=True)
class ExpectedField:
    name: str
    value: str
    required: bool = False


@dataclass(frozen=True)
class PredictedField:
    name: str
    value: str
    confidence: float | None = None


@dataclass(frozen=True)
class EvaluationDocument:
    document_id: str
    expected_type: str
    predicted_type: str | None = None
    classification_confidence: float | None = None
    expected_fields: tuple[ExpectedField, ...] = field(default_factory=tuple)
    predicted_fields: tuple[PredictedField, ...] = field(default_factory=tuple)


@dataclass(frozen=True)
class ClassificationMetrics:
    total: int
    correct: int
    accuracy: float
    labels: tuple[str, ...]
    confusion_matrix: dict[str, dict[str, int]]
    per_class: dict[str, dict[str, float]]


@dataclass(frozen=True)
class FieldEvaluation:
    document_id: str
    field_name: str
    expected_value: str
    predicted_value: str | None
    raw_exact_match: bool
    normalized_exact_match: bool
    confidence: float | None
    required: bool


@dataclass(frozen=True)
class ExtractionMetrics:
    expected_field_count: int
    predicted_field_count: int
    matched_field_count: int
    raw_exact_match_count: int
    normalized_exact_match_count: int
    missing_field_count: int
    unexpected_field_count: int
    raw_exact_match_rate: float
    normalized_exact_match_rate: float
    field_coverage: float
    fields: tuple[FieldEvaluation, ...]


JsonObject = dict[str, Any]
