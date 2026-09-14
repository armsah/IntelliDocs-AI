import unittest

from intellidocs_eval.classification import evaluate_classification
from intellidocs_eval.models import EvaluationDocument


class ClassificationTests(unittest.TestCase):
    def test_perfect_classification(self):
        documents = [
            EvaluationDocument(
                document_id="1",
                expected_type="invoice",
                predicted_type="invoice",
            ),
            EvaluationDocument(
                document_id="2",
                expected_type="purchase_order",
                predicted_type="purchase_order",
            ),
        ]

        result = evaluate_classification(documents)

        self.assertEqual(2, result.total)
        self.assertEqual(2, result.correct)
        self.assertEqual(1.0, result.accuracy)
        self.assertEqual(
            1.0,
            result.per_class["invoice"]["f1"],
        )

    def test_misclassification_reduces_metrics(self):
        documents = [
            EvaluationDocument(
                document_id="1",
                expected_type="invoice",
                predicted_type="invoice",
            ),
            EvaluationDocument(
                document_id="2",
                expected_type="purchase_order",
                predicted_type="invoice",
            ),
        ]

        result = evaluate_classification(documents)

        self.assertEqual(0.5, result.accuracy)
        self.assertEqual(
            0.5,
            result.per_class["invoice"]["precision"],
        )
        self.assertEqual(
            0.0,
            result.per_class["purchase_order"]["recall"],
        )

        def test_missing_prediction_is_not_counted_as_classification(self):
            documents = [
                EvaluationDocument(
                    document_id="1",
                    expected_type="invoice",
                )
            ]

            result = evaluate_classification(documents)

            self.assertEqual(0, result.total)
            self.assertEqual(0, result.correct)
            self.assertEqual((), result.labels)



    def test_missing_prediction_is_not_counted_as_classification(self):
        documents = [
            EvaluationDocument(
                document_id="1",
                expected_type="invoice",
            )
        ]

        result = evaluate_classification(documents)

        self.assertEqual(0, result.total)
        self.assertEqual(0, result.correct)
        self.assertEqual((), result.labels)
if __name__ == "__main__":
    unittest.main()
