import unittest

from intellidocs_eval.normalization import (
    normalize_field_value,
    normalize_identifier,
    normalize_number,
    normalize_text,
)


class NormalizationTests(unittest.TestCase):
    def test_text_normalization(self):
        self.assertEqual(
            "müller gmbh",
            normalize_text("  Müller   GmbH "),
        )

    def test_identifier_normalization(self):
        self.assertEqual(
            "inv20261001",
            normalize_identifier("INV-2026-1001"),
        )

    def test_german_number_normalization(self):
        self.assertEqual(
            "1234.56",
            normalize_number("1.234,56 €"),
        )

    def test_english_number_normalization(self):
        self.assertEqual(
            "1234.56",
            normalize_number("1,234.56"),
        )

    def test_invoice_number_uses_identifier_rules(self):
        self.assertEqual(
            "inv20261001",
            normalize_field_value(
                "invoiceNumber",
                "INV-2026-1001",
            ),
        )

    def test_total_uses_numeric_rules(self):
        self.assertEqual(
            "1234.56",
            normalize_field_value(
                "totalAmount",
                "1.234,56 €",
            ),
        )


if __name__ == "__main__":
    unittest.main()
