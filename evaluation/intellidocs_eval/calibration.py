from __future__ import annotations

from dataclasses import dataclass

from .models import EvaluationDocument, FieldEvaluation


@dataclass(frozen=True)
class CalibrationBin:
    lower_bound: float
    upper_bound: float
    count: int
    average_confidence: float
    accuracy: float
    calibration_error: float


@dataclass(frozen=True)
class CalibrationMetrics:
    sample_count: int
    expected_calibration_error: float
    bins: tuple[CalibrationBin, ...]


def evaluate_classification_calibration(
    documents: list[EvaluationDocument],
    bin_count: int = 10,
) -> CalibrationMetrics:
    samples: list[tuple[float, bool]] = []

    for document in documents:
        if document.classification_confidence is None:
            continue

        samples.append(
            (
                document.classification_confidence,
                document.expected_type == document.predicted_type,
            )
        )

    return _evaluate_samples(samples, bin_count)


def evaluate_field_calibration(
    fields: tuple[FieldEvaluation, ...],
    bin_count: int = 10,
) -> CalibrationMetrics:
    samples = [
        (
            field.confidence,
            field.normalized_exact_match,
        )
        for field in fields
        if field.confidence is not None
    ]

    return _evaluate_samples(samples, bin_count)


def _evaluate_samples(
    samples: list[tuple[float, bool]],
    bin_count: int,
) -> CalibrationMetrics:
    if bin_count <= 0:
        raise ValueError("bin_count must be greater than zero.")

    if not samples:
        return CalibrationMetrics(
            sample_count=0,
            expected_calibration_error=0.0,
            bins=(),
        )

    bins: list[CalibrationBin] = []
    total = len(samples)

    for index in range(bin_count):
        lower = index / bin_count
        upper = (index + 1) / bin_count

        if index == bin_count - 1:
            selected = [
                sample
                for sample in samples
                if lower <= sample[0] <= upper
            ]
        else:
            selected = [
                sample
                for sample in samples
                if lower <= sample[0] < upper
            ]

        if not selected:
            continue

        average_confidence = (
            sum(confidence for confidence, _ in selected)
            / len(selected)
        )

        accuracy = (
            sum(1 for _, correct in selected if correct)
            / len(selected)
        )

        error = abs(average_confidence - accuracy)

        bins.append(
            CalibrationBin(
                lower_bound=lower,
                upper_bound=upper,
                count=len(selected),
                average_confidence=average_confidence,
                accuracy=accuracy,
                calibration_error=error,
            )
        )

    expected_calibration_error = sum(
        (item.count / total) * item.calibration_error
        for item in bins
    )

    return CalibrationMetrics(
        sample_count=total,
        expected_calibration_error=expected_calibration_error,
        bins=tuple(bins),
    )
