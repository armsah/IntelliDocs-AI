import json
import tempfile
import unittest
from pathlib import Path

from intellidocs_eval.document_intelligence import (
    load_document_intelligence_fields,
    load_model_id,
)


class DocumentIntelligenceAdapterTests(unittest.TestCase):
    def test_loads_actual_p4_pascal_case_shape(self):
        payload = {
            "ModelId": "prebuilt-invoice",
            "Fields": [
                {
                    "Name": "InvoiceId",
                    "Content": "INV-100",
                    "Confidence": 0.98,
                    "BoundingRegions": [],
                },
                {
                    "Name": "InvoiceTotal",
                    "Content": "100.50 EUR",
                    "Confidence": 0.95,
                    "BoundingRegions": [],
                },
            ],
        }

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "result.json"

            path.write_text(
                json.dumps(payload),
                encoding="utf-8",
            )

            fields = load_document_intelligence_fields(
                path
            )

            model_id = load_model_id(path)

        self.assertEqual(
            "prebuilt-invoice",
            model_id,
        )

        self.assertEqual(2, len(fields))

        self.assertEqual(
            "invoiceNumber",
            fields[0].name,
        )

        self.assertEqual(
            "INV-100",
            fields[0].value,
        )

        self.assertEqual(
            "totalAmount",
            fields[1].name,
        )

        self.assertEqual(
            0.95,
            fields[1].confidence,
        )

    def test_empty_layout_fields_return_empty_tuple(self):
        payload = {
            "ModelId": "prebuilt-layout",
            "Fields": [],
        }

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "layout.json"

            path.write_text(
                json.dumps(payload),
                encoding="utf-8",
            )

            fields = load_document_intelligence_fields(
                path
            )

        self.assertEqual((), fields)


if __name__ == "__main__":
    unittest.main()
