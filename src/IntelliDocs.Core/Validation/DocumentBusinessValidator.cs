using System.Globalization;

namespace IntelliDocs.Core.Validation;

public static class DocumentBusinessValidator
{
    private static readonly IReadOnlyDictionary<
        string,
        string[]> RequiredFields =
        new Dictionary<string, string[]>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["invoice"] =
            [
                "invoiceNumber",
                "invoiceDate",
                "supplierName",
                "customerName",
                "currency",
                "totalAmount"
            ],
            ["purchase_order"] =
            [
                "purchaseOrderNumber",
                "orderDate",
                "buyerName",
                "supplierName",
                "currency",
                "totalAmount"
            ],
            ["delivery_note"] =
            [
                "deliveryNoteNumber",
                "deliveryDate",
                "supplierName",
                "customerName"
            ]
        };

    public static IReadOnlyList<ValidationIssue> Validate(
        string documentType,
        IReadOnlyList<NormalizedDocumentField> fields)
    {
        var issues = new List<ValidationIssue>();

        if (!RequiredFields.TryGetValue(
                documentType,
                out var requiredFields))
        {
            issues.Add(
                new ValidationIssue(
                    "MANUAL_REVIEW_DOCUMENT_TYPE",
                    $"Document type '{documentType}' is not eligible for structured auto-approval.",
                    ValidationSeverity.Warning));

            return issues;
        }

        var fieldsByName =
            fields
                .GroupBy(
                    x => x.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase);

        foreach (var requiredField in requiredFields)
        {
            if (!fieldsByName.TryGetValue(
                    requiredField,
                    out var field) ||
                string.IsNullOrWhiteSpace(
                    field.NormalizedValue))
            {
                issues.Add(
                    new ValidationIssue(
                        "REQUIRED_FIELD_MISSING",
                        $"Required field '{requiredField}' is missing.",
                        ValidationSeverity.Error,
                        requiredField));
            }
        }

        ValidateDate(
            fieldsByName,
            GetDateField(documentType),
            issues);

        ValidateAmount(
            fieldsByName,
            issues);

        ValidateCurrency(
            fieldsByName,
            issues);

        return issues;
    }

    public static IReadOnlyList<string> GetRequiredFields(
        string documentType)
    {
        return RequiredFields.TryGetValue(
                documentType,
                out var requiredFields)
            ? requiredFields
            : [];
    }

    private static string? GetDateField(
        string documentType)
    {
        return documentType.ToLowerInvariant() switch
        {
            "invoice" => "invoiceDate",
            "purchase_order" => "orderDate",
            "delivery_note" => "deliveryDate",
            _ => null
        };
    }

    private static void ValidateDate(
        IReadOnlyDictionary<
            string,
            NormalizedDocumentField> fields,
        string? fieldName,
        ICollection<ValidationIssue> issues)
    {
        if (fieldName is null ||
            !fields.TryGetValue(
                fieldName,
                out var field) ||
            string.IsNullOrWhiteSpace(
                field.NormalizedValue))
        {
            return;
        }

        if (!DateOnly.TryParseExact(
                field.NormalizedValue,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            issues.Add(
                new ValidationIssue(
                    "INVALID_DATE",
                    $"Field '{fieldName}' is not a valid supported date.",
                    ValidationSeverity.Error,
                    fieldName));
        }
    }

    private static void ValidateAmount(
        IReadOnlyDictionary<
            string,
            NormalizedDocumentField> fields,
        ICollection<ValidationIssue> issues)
    {
        if (!fields.TryGetValue(
                "totalAmount",
                out var field) ||
            string.IsNullOrWhiteSpace(
                field.NormalizedValue))
        {
            return;
        }

        if (!decimal.TryParse(
                field.NormalizedValue,
                NumberStyles.Number |
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            issues.Add(
                new ValidationIssue(
                    "INVALID_TOTAL_AMOUNT",
                    "Total amount is not a valid decimal value.",
                    ValidationSeverity.Error,
                    "totalAmount"));

            return;
        }

        if (amount < 0)
        {
            issues.Add(
                new ValidationIssue(
                    "NEGATIVE_TOTAL_AMOUNT",
                    "Total amount must not be negative.",
                    ValidationSeverity.Critical,
                    "totalAmount"));
        }
    }

    private static void ValidateCurrency(
        IReadOnlyDictionary<
            string,
            NormalizedDocumentField> fields,
        ICollection<ValidationIssue> issues)
    {
        if (!fields.TryGetValue(
                "currency",
                out var field) ||
            string.IsNullOrWhiteSpace(
                field.NormalizedValue))
        {
            return;
        }

        var currency =
            field.NormalizedValue;

        if (currency.Length != 3 ||
            !currency.All(
                character =>
                    character is >= 'A' and <= 'Z'))
        {
            issues.Add(
                new ValidationIssue(
                    "INVALID_CURRENCY",
                    "Currency must be a three-letter uppercase code.",
                    ValidationSeverity.Error,
                    "currency"));
        }
    }
}
