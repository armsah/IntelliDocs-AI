using IntelliDocs.Core.Validation;

namespace IntelliDocs.UnitTests.Validation;

public sealed class DocumentFieldNormalizerTests
{
    [Theory]
    [InlineData("15.09.2026", "2026-09-15")]
    [InlineData("5.9.2026", "2026-09-05")]
    [InlineData("2026-09-15", "2026-09-15")]
    public void NormalizesSupportedDates(
        string raw,
        string expected)
    {
        Assert.Equal(
            expected,
            DocumentFieldNormalizer.NormalizeValue(
                "invoiceDate",
                raw));
    }

    [Theory]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1,234.56", "1234.56")]
    [InlineData("1234,56", "1234.56")]
    [InlineData("1234.56", "1234.56")]
    public void NormalizesGermanAndEnglishAmounts(
        string raw,
        string expected)
    {
        Assert.Equal(
            expected,
            DocumentFieldNormalizer.NormalizeValue(
                "totalAmount",
                raw));
    }

    [Theory]
    [InlineData("€", "EUR")]
    [InlineData("euro", "EUR")]
    [InlineData("eur", "EUR")]
    [InlineData("$", "USD")]
    [InlineData("£", "GBP")]
    public void NormalizesCurrencies(
        string raw,
        string expected)
    {
        Assert.Equal(
            expected,
            DocumentFieldNormalizer.NormalizeValue(
                "currency",
                raw));
    }

    [Fact]
    public void CollapsesWhitespace()
    {
        Assert.Equal(
            "Example Supplier GmbH",
            DocumentFieldNormalizer.NormalizeValue(
                "supplierName",
                "  Example   Supplier   GmbH  "));
    }

    [Fact]
    public void NormalizesIdentifiersToUppercase()
    {
        Assert.Equal(
            "INV-2026-001",
            DocumentFieldNormalizer.NormalizeValue(
                "invoiceNumber",
                " inv-2026-001 "));
    }
}
