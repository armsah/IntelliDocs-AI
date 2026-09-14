import unittest

from intellidocs_eval.calibration import (
    evaluate_classification_calibration,
    evaluate_field_calibration,
)
from intellidocs_eval.models import (
    EvaluationDocument,
    FieldEvaluation,
)


class CalibrationTests(unittest.TestCase):
    def test_perfect_confident_classification_is_calibrated(self):
        documents = [
            EvaluationDocument(
                document_id="1",
                expected_type="invoice",
                predicted_type="invoice",
                classification_confidence=1.0,
            )
        ]

        result = evaluate_classification_calibration(
            documents
        )

        self.assertEqual(1, result.sample_count)
        self.assertEqual(
            0.0,
            result.expected_calibration_error,
        )

    def test_incorrect_high_confidence_has_large_error(self):
        documents = [
            EvaluationDocument(
                document_id="1",
                expected_type="invoice",
                predicted_type="contract",
                classification_confidence=0.9,
            )
        ]

        result = evaluate_classification_calibration(
            documents
        )

        self.assertAlmostEqual(
            0.9,
            result.expected_calibration_error,
        )

    def test_field_calibration_uses_normalized_match(self):
        fields = (
            FieldEvaluation(
                document_id="1",
                field_name="invoiceNumber",
                expected_value="INV-1",
                predicted_value="INV 1",
                raw_exact_match=False,
                normalized_exact_match=True,
                confidence=1.0,
                required=True,
            ),
        )

        result = evaluate_field_calibration(fields)

        self.assertEqual(1, result.sample_count)
        self.assertEqual(
            0.0,
            result.expected_calibration_error,
        )


if __name__ == "__main__":
    unittest.main()
