from __future__ import annotations

import json
from dataclasses import asdict
from pathlib import Path

from .analysis import (
    build_field_summaries,
    build_type_summaries,
)
from .calibration import (
    evaluate_classification_calibration,
    evaluate_field_calibration,
)
from .classification import evaluate_classification
from .extraction import evaluate_extraction
from .models import EvaluationDocument


def build_report(
    documents: list[EvaluationDocument],
) -> dict[str, object]:
    classification = evaluate_classification(documents)
    extraction = evaluate_extraction(documents)

    classification_calibration = (
        evaluate_classification_calibration(documents)
    )

    field_calibration = evaluate_field_calibration(
        extraction.fields
    )

    field_summaries = build_field_summaries(
        documents,
        extraction,
    )

    type_summaries = build_type_summaries(
        documents,
        extraction,
    )

    return {
        "documentCount": len(documents),
        "classification": asdict(classification),
        "extraction": asdict(extraction),
        "classificationCalibration": asdict(
            classification_calibration
        ),
        "fieldCalibration": asdict(field_calibration),
        "fieldSummaries": {
            name: asdict(summary)
            for name, summary in field_summaries.items()
        },
        "typeSummaries": {
            name: asdict(summary)
            for name, summary in type_summaries.items()
        },
    }


def write_json_report(
    report: dict[str, object],
    path: str | Path,
) -> None:
    destination = Path(path)
    destination.parent.mkdir(parents=True, exist_ok=True)

    with destination.open("w", encoding="utf-8") as stream:
        json.dump(
            report,
            stream,
            ensure_ascii=False,
            indent=2,
        )
        stream.write("\n")


def write_markdown_report(
    documents: list[EvaluationDocument],
    report: dict[str, object],
    path: str | Path,
) -> None:
    destination = Path(path)
    destination.parent.mkdir(parents=True, exist_ok=True)

    classification = report["classification"]
    extraction = report["extraction"]

    classification_calibration = report[
        "classificationCalibration"
    ]

    field_calibration = report["fieldCalibration"]

    classification_accuracy_text = (
        f"{classification['accuracy']:.4f}"
        if classification["total"] > 0
        else "not evaluated"
    )

    lines = [
        "# IntelliDocs AI Evaluation Report",
        "",
        f"Documents evaluated: **{len(documents)}**",
        "",
        "## Classification",
        "",
        (
            "- Accuracy: "
            f"**{classification_accuracy_text}**"
        ),
        (
            "- Correct: "
            f"**{classification['correct']} / "
            f"{classification['total']}**"
        ),
        "",
        "### Per-class metrics",
        "",
        "| Class | Precision | Recall | F1 | Support |",
        "| --- | ---: | ---: | ---: | ---: |",
    ]

    for label, metrics in classification[
        "per_class"
    ].items():
        lines.append(
            "| "
            f"{label} | "
            f"{metrics['precision']:.4f} | "
            f"{metrics['recall']:.4f} | "
            f"{metrics['f1']:.4f} | "
            f"{int(metrics['support'])} |"
        )

    lines.extend(
        [
            "",
            "## Extraction",
            "",
            (
                "- Field coverage: "
                f"**{extraction['field_coverage']:.4f}**"
            ),
            (
                "- Raw exact-match rate: "
                f"**{extraction['raw_exact_match_rate']:.4f}**"
            ),
            (
                "- Normalized exact-match rate: "
                f"**{extraction['normalized_exact_match_rate']:.4f}**"
            ),
            (
                "- Missing fields: "
                f"**{extraction['missing_field_count']}**"
            ),
            (
                "- Unexpected fields: "
                f"**{extraction['unexpected_field_count']}**"
            ),
            "",
            "### Per-field metrics",
            "",
            (
                "| Field | Expected | Coverage | "
                "Normalized accuracy | Avg confidence |"
            ),
            "| --- | ---: | ---: | ---: | ---: |",
        ]
    )

    for name, metrics in report[
        "fieldSummaries"
    ].items():
        average_confidence = metrics[
            "average_confidence"
        ]

        confidence_text = (
            f"{average_confidence:.4f}"
            if average_confidence is not None
            else "-"
        )

        lines.append(
            "| "
            f"{name} | "
            f"{metrics['expected_count']} | "
            f"{metrics['coverage']:.4f} | "
            f"{metrics['normalized_accuracy']:.4f} | "
            f"{confidence_text} |"
        )

    lines.extend(
        [
            "",
            "## Per-document-type summary",
            "",
            (
                "| Type | Documents | Classification accuracy | "
                "Field coverage | Normalized extraction |"
            ),
            "| --- | ---: | ---: | ---: | ---: |",
        ]
    )

    for name, metrics in report[
        "typeSummaries"
    ].items():
        classification_accuracy = metrics[
            "classification_accuracy"
        ]

        classification_text = (
            f"{classification_accuracy:.4f}"
            if classification_accuracy is not None
            else "not evaluated"
        )

        lines.append(
            "| "
            f"{name} | "
            f"{metrics['document_count']} | "
            f"{classification_text} | "
            f"{metrics['field_coverage']:.4f} | "
            f"{metrics['normalized_exact_match_rate']:.4f} |"
        )

    lines.extend(
        [
            "",
            "## Calibration",
            "",
            (
                "- Classification ECE: "
                f"**{classification_calibration['expected_calibration_error']:.4f}**"
            ),
            (
                "- Field ECE: "
                f"**{field_calibration['expected_calibration_error']:.4f}**"
            ),
            "",
            "## Notes",
            "",
            (
                "Metrics describe only the supplied evaluation dataset. "
                "Dataset provenance and representativeness must be reviewed "
                "before treating results as production model-quality evidence."
            ),
        ]
    )

    destination.write_text(
        "\n".join(lines) + "\n",
        encoding="utf-8",
    )
