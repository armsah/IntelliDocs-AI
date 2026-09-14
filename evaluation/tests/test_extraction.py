import unittest

from intellidocs_eval.extraction import evaluate_extraction
from intellidocs_eval.models import (
    EvaluationDocument,
    ExpectedField,
    PredictedField,
)


class ExtractionTests(unittest.TestCase):
    def test_normalized_exact_match(self):
        documents = [
            EvaluationDocument(
                document_id="invoice-1",
                expected_type="invoice",
                expected_fields=(
                    ExpectedField(
                        "invoiceNumber",
                        "INV-2026-1001",
                        required=True,
                    ),
                    ExpectedField(
                        "totalAmount",
                        "1.234,56 €",
                        required=True,
                    ),
                ),
                predicted_fields=(
                    PredictedField(
                        "invoiceNumber",
                        "INV 2026 1001",
                        0.98,
                    ),
                    PredictedField(
                        "totalAmount",
                        "1234.56",
                        0.96,
                    ),
                ),
            )
        ]

        result = evaluate_extraction(documents)

        self.assertEqual(2, result.expected_field_count)
        self.assertEqual(2, result.matched_field_count)
        self.assertEqual(0, result.raw_exact_match_count)
        self.assertEqual(2, result.normalized_exact_match_count)
        self.assertEqual(1.0, result.normalized_exact_match_rate)
        self.assertEqual(1.0, result.field_coverage)

    def test_missing_field_is_reported(self):
        documents = [
            EvaluationDocument(
                document_id="invoice-1",
                expected_type="invoice",
                expected_fields=(
                    ExpectedField(
                        "invoiceNumber",
                        "INV-1",
                        required=True,
                    ),
                ),
            )
        ]

        result = evaluate_extraction(documents)

        self.assertEqual(1, result.missing_field_count)
        self.assertEqual(0.0, result.field_coverage)


if __name__ == "__main__":
    unittest.main()
