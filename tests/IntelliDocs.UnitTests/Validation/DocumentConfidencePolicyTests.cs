using IntelliDocs.Core.DocumentClassification;
using IntelliDocs.Core.DocumentIntelligence;
using IntelliDocs.Core.Validation;

namespace IntelliDocs.UnitTests.Validation;

public sealed class DocumentConfidencePolicyTests
{
    [Fact]
    public void ValidInvoiceAtAutoApproveBoundaryIsApproved()
    {
        var result =
            EvaluateInvoice(
                classificationConfidence: 0.90,
                fieldConfidence: 0.95);

        Assert.Equal(
            DocumentRoutingDecision.Approved,
            result.Decision);

        Assert.Equal(
            0.90,
            result.PolicyConfidence,
            precision: 10);

        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidInvoiceBelowAutoApproveBoundaryNeedsReview()
    {
        var result =
            EvaluateInvoice(
                classificationConfidence: 0.899,
                fieldConfidence: 0.95);

        Assert.Equal(
            DocumentRoutingDecision.NeedsReview,
            result.Decision);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "REVIEW_CONFIDENCE");
    }

    [Fact]
    public void ConfidenceAtReviewBoundaryNeedsReviewWithoutLowConfidenceWarning()
    {
        var result =
            EvaluateInvoice(
                classificationConfidence: 0.700,
                fieldConfidence: 0.95);

        Assert.Equal(
            DocumentRoutingDecision.NeedsReview,
            result.Decision);

        Assert.DoesNotContain(
            result.Issues,
            issue =>
                issue.Code ==
                "LOW_CONFIDENCE");

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "REVIEW_CONFIDENCE");
    }

    [Fact]
    public void ConfidenceBelowReviewBoundaryNeedsReviewWithWarning()
    {
        var result =
            EvaluateInvoice(
                classificationConfidence: 0.699,
                fieldConfidence: 0.95);

        Assert.Equal(
            DocumentRoutingDecision.NeedsReview,
            result.Decision);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "LOW_CONFIDENCE");
    }

    [Fact]
    public void LowestMandatoryFieldConfidenceControlsPolicyConfidence()
    {
        var fields =
            ValidInvoiceFields(
                confidence: 0.96)
                .Select(
                    field =>
                        field.Name ==
                        "supplierName"
                            ? field with
                            {
                                Confidence = 0.82
                            }
                            : field)
                .ToArray();

        var result =
            Evaluate(
                "invoice",
                0.97,
                fields);

        Assert.Equal(
            0.82,
            result.PolicyConfidence,
            precision: 10);

        Assert.Equal(
            DocumentRoutingDecision.NeedsReview,
            result.Decision);
    }

    [Fact]
    public void MissingMandatoryFieldNeedsReview()
    {
        var fields =
            ValidInvoiceFields()
                .Where(
                    field =>
                        field.Name !=
                        "currency")
                .ToArray();

        var result =
            Evaluate(
                "invoice",
                0.99,
                fields);

        Assert.Equal(
            DocumentRoutingDecision.NeedsReview,
            result.Decision);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    "REQUIRED_FIELD_MISSING" &&
                issue.FieldName ==
                    "currency");
    }

    [Fact]
    public void CriticalBusinessConflictOverridesHighConfidence()
    {
        var fields =
            ValidInvoiceFields()
                .Select(
                    field =>
                        field.Name ==
                        "totalAmount"
                            ? field with
                            {
                                Content = "-125.00"
                            }
                            : field)
                .ToArray();

        var result =
            Evaluate(
                "invoice",
                0.99,
                fields);

        Assert.Equal(
            DocumentRoutingDecision.NeedsReview,
            result.Decision);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    "NEGATIVE_TOTAL_AMOUNT" &&
                issue.Severity ==
                    ValidationSeverity.Critical);
    }

    [Theory]
    [InlineData("contract")]
    [InlineData("form")]
    [InlineData("unknown")]
    public void NonStructuredDocumentTypesRequireManualReview(
        string documentType)
    {
        var result =
            Evaluate(
                documentType,
                0.99,
                []);

        Assert.Equal(
            DocumentRoutingDecision.NeedsReview,
            result.Decision);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                "MANUAL_REVIEW_DOCUMENT_TYPE");
    }

    [Fact]
    public void ValidPurchaseOrderCanBeApproved()
    {
        var fields =
            new[]
            {
                Field(
                    "purchaseOrderNumber",
                    "po-2026-001"),
                Field(
                    "orderDate",
                    "15.09.2026"),
                Field(
                    "buyerName",
                    "Example Buyer GmbH"),
                Field(
                    "supplierName",
                    "Example Supplier GmbH"),
                Field(
                    "currency",
                    "eur"),
                Field(
                    "totalAmount",
                    "1.234,56")
            };

        var result =
            Evaluate(
                "purchase_order",
                0.96,
                fields);

        Assert.Equal(
            DocumentRoutingDecision.Approved,
            result.Decision);

        Assert.Equal(
            "PO-2026-001",
            GetValue(
                result,
                "purchaseOrderNumber"));

        Assert.Equal(
            "2026-09-15",
            GetValue(
                result,
                "orderDate"));

        Assert.Equal(
            "EUR",
            GetValue(
                result,
                "currency"));

        Assert.Equal(
            "1234.56",
            GetValue(
                result,
                "totalAmount"));
    }

    [Fact]
    public void ValidDeliveryNoteCanBeApproved()
    {
        var fields =
            new[]
            {
                Field(
                    "deliveryNoteNumber",
                    "dn-100"),
                Field(
                    "deliveryDate",
                    "2026-09-15"),
                Field(
                    "supplierName",
                    "Supplier GmbH"),
                Field(
                    "customerName",
                    "Customer GmbH")
            };

        var result =
            Evaluate(
                "delivery_note",
                0.95,
                fields);

        Assert.Equal(
            DocumentRoutingDecision.Approved,
            result.Decision);
    }

    private static DocumentValidationResult
        EvaluateInvoice(
            double classificationConfidence,
            double fieldConfidence)
    {
        return Evaluate(
            "invoice",
            classificationConfidence,
            ValidInvoiceFields(
                fieldConfidence));
    }

    private static DocumentValidationResult Evaluate(
        string documentType,
        double classificationConfidence,
        IReadOnlyList<DocumentExtractedField> fields)
    {
        var classification =
            new DocumentClassificationResult(
                "test-classifier",
                documentType,
                classificationConfidence);

        var analysis =
            new DocumentAnalysisResult(
                "test-model",
                "1",
                string.Empty,
                [],
                fields,
                []);

        return DocumentConfidencePolicy.Evaluate(
            classification,
            analysis);
    }

    private static DocumentExtractedField[]
        ValidInvoiceFields(
            double confidence = 0.95)
    {
        return
        [
            Field(
                "invoiceNumber",
                "inv-2026-001",
                confidence),
            Field(
                "invoiceDate",
                "15.09.2026",
                confidence),
            Field(
                "supplierName",
                "Supplier GmbH",
                confidence),
            Field(
                "customerName",
                "Customer GmbH",
                confidence),
            Field(
                "currency",
                "€",
                confidence),
            Field(
                "totalAmount",
                "1.234,56",
                confidence)
        ];
    }

    private static DocumentExtractedField Field(
        string name,
        string content,
        double confidence = 0.95)
    {
        return new DocumentExtractedField(
            name,
            content,
            confidence,
            []);
    }

    private static string? GetValue(
        DocumentValidationResult result,
        string fieldName)
    {
        return result.Fields
            .Single(
                field =>
                    field.Name ==
                    fieldName)
            .NormalizedValue;
    }
}
