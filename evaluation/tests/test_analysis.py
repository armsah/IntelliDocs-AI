import unittest

from intellidocs_eval.analysis import (
    build_field_summaries,
    build_type_summaries,
)
from intellidocs_eval.extraction import evaluate_extraction
from intellidocs_eval.models import (
    EvaluationDocument,
    ExpectedField,
    PredictedField,
)


class AnalysisTests(unittest.TestCase):
    def test_field_summary_reports_accuracy_and_coverage(self):
        documents = [
            EvaluationDocument(
                document_id="1",
                expected_type="invoice",
                predicted_type="invoice",
                expected_fields=(
                    ExpectedField(
                        "invoiceNumber",
                        "INV-1",
                        required=True,
                    ),
                ),
                predicted_fields=(
                    PredictedField(
                        "invoiceNumber",
                        "INV 1",
                        0.9,
                    ),
                ),
            )
        ]

        extraction = evaluate_extraction(documents)

        summaries = build_field_summaries(
            documents,
            extraction,
        )

        summary = summaries["invoiceNumber"]

        self.assertEqual(1.0, summary.coverage)
        self.assertEqual(
            1.0,
            summary.normalized_accuracy,
        )
        self.assertEqual(
            0.9,
            summary.average_confidence,
        )

    def test_type_summary_reports_classification_failure(self):
        documents = [
            EvaluationDocument(
                document_id="1",
                expected_type="form",
                predicted_type="contract",
            )
        ]

        extraction = evaluate_extraction(documents)

        summary = build_type_summaries(
            documents,
            extraction,
        )["form"]

        self.assertEqual(1, summary.document_count)
        self.assertEqual(
            1,
            summary.classification_evaluated_count,
        )
        self.assertEqual(
            0.0,
            summary.classification_accuracy,
        )

    def test_type_summary_marks_missing_classifier_as_not_evaluated(self):
        documents = [
            EvaluationDocument(
                document_id="1",
                expected_type="invoice",
            )
        ]

        extraction = evaluate_extraction(documents)

        summary = build_type_summaries(
            documents,
            extraction,
        )["invoice"]

        self.assertEqual(
            0,
            summary.classification_evaluated_count,
        )
        self.assertIsNone(
            summary.classification_accuracy
        )


if __name__ == "__main__":
    unittest.main()
