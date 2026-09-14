using IntelliDocs.Core.DocumentIntelligence;

namespace IntelliDocs.Core.DocumentClassification;

public static class DocumentAnalysisRouter
{
    private static readonly IReadOnlyList<string>
        PurchaseOrderQueryFields =
        [
            "purchaseOrderNumber",
            "orderDate",
            "buyerName",
            "supplierName",
            "currency",
            "totalAmount"
        ];

    private static readonly IReadOnlyList<string>
        DeliveryNoteQueryFields =
        [
            "deliveryNoteNumber",
            "deliveryDate",
            "supplierName",
            "customerName"
        ];

    public static DocumentAnalysisRoute Resolve(
        string documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            throw new ArgumentException(
                "Document type is required.",
                nameof(documentType));
        }

        return documentType
            .Trim()
            .ToLowerInvariant()
            switch
        {
            "invoice" =>
                new DocumentAnalysisRoute(
                    DocumentAnalysisModel.Invoice,
                    null),

            "purchase_order" =>
                new DocumentAnalysisRoute(
                    DocumentAnalysisModel.Layout,
                    PurchaseOrderQueryFields),

            "delivery_note" =>
                new DocumentAnalysisRoute(
                    DocumentAnalysisModel.Layout,
                    DeliveryNoteQueryFields),

            "contract" =>
                new DocumentAnalysisRoute(
                    DocumentAnalysisModel.Layout,
                    null),

            "form" =>
                new DocumentAnalysisRoute(
                    DocumentAnalysisModel.Layout,
                    null),

            _ => throw new ArgumentException(
                $"Unsupported classified document type '{documentType}'.",
                nameof(documentType))
        };
    }
}
