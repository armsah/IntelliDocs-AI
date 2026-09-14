from __future__ import annotations

import argparse

from .dataset import load_evaluation_dataset
from .reporting import (
    build_report,
    write_json_report,
    write_markdown_report,
)


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Evaluate IntelliDocs document classification "
            "and field extraction results."
        )
    )

    parser.add_argument(
        "dataset",
        help="Path to evaluation dataset JSON.",
    )

    parser.add_argument(
        "--json-output",
        required=True,
        help="Path to generated JSON report.",
    )

    parser.add_argument(
        "--markdown-output",
        required=True,
        help="Path to generated Markdown report.",
    )

    args = parser.parse_args()

    documents = load_evaluation_dataset(args.dataset)

    report = build_report(documents)

    write_json_report(
        report,
        args.json_output,
    )

    write_markdown_report(
        documents,
        report,
        args.markdown_output,
    )

    print(
        f"Evaluated {len(documents)} document(s)."
    )

    classification = report["classification"]

    if classification["total"] > 0:
        print(
            "Classification accuracy: "
            f"{classification['accuracy']:.4f}"
        )
    else:
        print(
            "Classification accuracy: not evaluated"
        )

    print(
        "Normalized extraction exact match: "
        f"{report['extraction']['normalized_exact_match_rate']:.4f}"
    )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
