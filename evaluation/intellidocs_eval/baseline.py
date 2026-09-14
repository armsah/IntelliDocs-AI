from __future__ import annotations

import argparse
import json
from pathlib import Path

from .document_intelligence import (
    load_document_intelligence_fields,
)


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Combine IntelliDocs ground truth with normalized "
            "Document Intelligence extraction output."
        )
    )

    parser.add_argument(
        "ground_truth",
        help="Ground-truth JSON document.",
    )

    parser.add_argument(
        "prediction",
        help="Normalized Document Intelligence JSON result.",
    )

    parser.add_argument(
        "output",
        help="Evaluation dataset JSON output.",
    )

    args = parser.parse_args()

    ground_truth_path = Path(args.ground_truth)

    with ground_truth_path.open(
        "r",
        encoding="utf-8-sig",
    ) as stream:
        ground_truth = json.load(stream)

    if not isinstance(ground_truth, dict):
        raise ValueError(
            "Ground truth must be a JSON object."
        )

    predicted_fields = (
        load_document_intelligence_fields(
            args.prediction
        )
    )

    document = dict(ground_truth)

    # No predictedType is added here. The P4 prebuilt/layout
    # models do not constitute a document classifier.
    document.pop("predictedType", None)
    document.pop("classificationConfidence", None)

    document["predictedFields"] = [
        {
            "name": field.name,
            "value": field.value,
            "confidence": field.confidence,
        }
        for field in predicted_fields
    ]

    payload = {
        "datasetName": "p6-document-intelligence-extraction-baseline",
        "purpose": (
            "Extraction-only baseline comparing labeled ground truth "
            "with normalized Azure Document Intelligence output."
        ),
        "documents": [document],
    }

    output_path = Path(args.output)

    output_path.parent.mkdir(
        parents=True,
        exist_ok=True,
    )

    output_path.write_text(
        json.dumps(
            payload,
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )

    print(
        f"Wrote extraction baseline with "
        f"{len(predicted_fields)} predicted field(s)."
    )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
