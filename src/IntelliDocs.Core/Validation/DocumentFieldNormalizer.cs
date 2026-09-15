using System.Globalization;
using System.Text.RegularExpressions;
using IntelliDocs.Core.DocumentIntelligence;

namespace IntelliDocs.Core.Validation;

public static partial class DocumentFieldNormalizer
{
    private static readonly HashSet<string> DateFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "invoiceDate",
            "orderDate",
            "deliveryDate"
        };

    private static readonly HashSet<string> AmountFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "totalAmount"
        };

    private static readonly HashSet<string> CurrencyFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "currency"
        };

    private static readonly HashSet<string> IdentifierFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "invoiceNumber",
            "purchaseOrderNumber",
            "deliveryNoteNumber"
        };

    public static IReadOnlyList<NormalizedDocumentField> Normalize(
        IReadOnlyList<DocumentExtractedField> fields)
    {
        return fields
            .Select(
                field =>
                    new NormalizedDocumentField(
                        field.Name,
                        field.Content,
                        NormalizeValue(
                            field.Name,
                            field.Content),
                        field.Confidence))
            .ToArray();
    }

    public static string? NormalizeValue(
        string fieldName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed =
            WhitespaceRegex()
                .Replace(value.Trim(), " ");

        if (DateFields.Contains(fieldName))
        {
            return NormalizeDate(trimmed);
        }

        if (AmountFields.Contains(fieldName))
        {
            return NormalizeAmount(trimmed);
        }

        if (CurrencyFields.Contains(fieldName))
        {
            return NormalizeCurrency(trimmed);
        }

        if (IdentifierFields.Contains(fieldName))
        {
            return trimmed.ToUpperInvariant();
        }

        return trimmed;
    }

    private static string NormalizeDate(string value)
    {
        var formats = new[]
        {
            "yyyy-MM-dd",
            "dd.MM.yyyy",
            "d.M.yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "MM/dd/yyyy",
            "M/d/yyyy"
        };

        if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exactDate))
        {
            return exactDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static string NormalizeAmount(string value)
    {
        var compact =
            value.Replace(" ", string.Empty);

        if (compact.Contains(',') &&
            compact.Contains('.'))
        {
            if (compact.LastIndexOf(',') >
                compact.LastIndexOf('.'))
            {
                compact =
                    compact.Replace(".", string.Empty)
                        .Replace(',', '.');
            }
            else
            {
                compact =
                    compact.Replace(",", string.Empty);
            }
        }
        else if (compact.Contains(','))
        {
            compact =
                compact.Replace(',', '.');
        }

        if (decimal.TryParse(
                compact,
                NumberStyles.Number |
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return amount.ToString(
                "0.############################",
                CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static string NormalizeCurrency(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "€" => "EUR",
            "EURO" => "EUR",
            "$" => "USD",
            "US$" => "USD",
            "£" => "GBP",
            _ => value.Trim().ToUpperInvariant()
        };
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
