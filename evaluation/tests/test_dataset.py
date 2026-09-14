import json
import tempfile
import unittest
from pathlib import Path

from intellidocs_eval.dataset import load_evaluation_dataset


class DatasetTests(unittest.TestCase):
    def test_loads_dataset(self):
        payload = {
            "documents": [
                {
                    "documentId": "invoice-1",
                    "expectedType": "invoice",
                    "predictedType": "invoice",
                    "classificationConfidence": 0.98,
                    "expectedFields": [
                        {
                            "name": "invoiceNumber",
                            "value": "INV-1",
                            "required": True,
                        }
                    ],
                    "predictedFields": [
                        {
                            "name": "invoiceNumber",
                            "value": "INV-1",
                            "confidence": 0.99,
                        }
                    ],
                }
            ]
        }

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "dataset.json"

            path.write_text(
                json.dumps(payload),
                encoding="utf-8",
            )

            documents = load_evaluation_dataset(path)

        self.assertEqual(1, len(documents))
        self.assertEqual(
            "invoice",
            documents[0].expected_type,
        )
        self.assertEqual(
            0.98,
            documents[0].classification_confidence,
        )
        self.assertEqual(
            "invoiceNumber",
            documents[0].expected_fields[0].name,
        )

    def test_rejects_invalid_confidence(self):
        payload = {
            "documents": [
                {
                    "documentId": "invoice-1",
                    "expectedType": "invoice",
                    "classificationConfidence": 1.5,
                }
            ]
        }

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "dataset.json"

            path.write_text(
                json.dumps(payload),
                encoding="utf-8",
            )

            with self.assertRaises(ValueError):
                load_evaluation_dataset(path)


if __name__ == "__main__":
    unittest.main()
