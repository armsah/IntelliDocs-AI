from __future__ import annotations

from .models import ClassificationMetrics, EvaluationDocument


def evaluate_classification(
    documents: list[EvaluationDocument],
) -> ClassificationMetrics:
    evaluated = [
        document
        for document in documents
        if document.predicted_type is not None
    ]

    if not evaluated:
        return ClassificationMetrics(
            total=0,
            correct=0,
            accuracy=0.0,
            labels=(),
            confusion_matrix={},
            per_class={},
        )

    labels = sorted(
        {
            document.expected_type
            for document in evaluated
        }
        | {
            document.predicted_type
            for document in evaluated
            if document.predicted_type is not None
        }
    )

    confusion: dict[str, dict[str, int]] = {
        expected: {
            predicted: 0
            for predicted in labels
        }
        for expected in labels
    }

    correct = 0

    for document in evaluated:
        predicted = document.predicted_type

        if predicted is None:
            continue

        confusion[
            document.expected_type
        ][predicted] += 1

        if document.expected_type == predicted:
            correct += 1

    per_class: dict[str, dict[str, float]] = {}

    for label in labels:
        true_positive = sum(
            1
            for document in evaluated
            if document.expected_type == label
            and document.predicted_type == label
        )

        false_positive = sum(
            1
            for document in evaluated
            if document.expected_type != label
            and document.predicted_type == label
        )

        false_negative = sum(
            1
            for document in evaluated
            if document.expected_type == label
            and document.predicted_type != label
        )

        precision = (
            true_positive / (true_positive + false_positive)
            if true_positive + false_positive
            else 0.0
        )

        recall = (
            true_positive / (true_positive + false_negative)
            if true_positive + false_negative
            else 0.0
        )

        f1 = (
            2 * precision * recall / (precision + recall)
            if precision + recall
            else 0.0
        )

        support = sum(
            1
            for document in evaluated
            if document.expected_type == label
        )

        predicted_count = sum(
            1
            for document in evaluated
            if document.predicted_type == label
        )

        per_class[label] = {
            "precision": precision,
            "recall": recall,
            "f1": f1,
            "support": float(support),
            "predicted": float(predicted_count),
        }

    return ClassificationMetrics(
        total=len(evaluated),
        correct=correct,
        accuracy=correct / len(evaluated),
        labels=tuple(labels),
        confusion_matrix=confusion,
        per_class=per_class,
    )
