using IntelliDocs.Core.DocumentClassification;
using IntelliDocs.Core.DocumentIntelligence;

namespace IntelliDocs.UnitTests.DocumentClassification;

public sealed class DocumentAnalysisRouterTests
{
    [Fact]
    public void Invoice_UsesPrebuiltInvoice()
    {
        var route =
            DocumentAnalysisRouter.Resolve("invoice");

        Assert.Equal(
            DocumentAnalysisModel.Invoice,
            route.Model);

        Assert.Null(route.QueryFields);
    }

    [Fact]
    public void PurchaseOrder_UsesLayoutWithExpectedQueryFields()
    {
        var route =
            DocumentAnalysisRouter.Resolve(
                "purchase_order");

        Assert.Equal(
            DocumentAnalysisModel.Layout,
            route.Model);

        Assert.Equal(
            [
                "purchaseOrderNumber",
                "orderDate",
                "buyerName",
                "supplierName",
                "currency",
                "totalAmount"
            ],
            route.QueryFields);
    }

    [Fact]
    public void DeliveryNote_UsesLayoutWithExpectedQueryFields()
    {
        var route =
            DocumentAnalysisRouter.Resolve(
                "delivery_note");

        Assert.Equal(
            DocumentAnalysisModel.Layout,
            route.Model);

        Assert.Equal(
            [
                "deliveryNoteNumber",
                "deliveryDate",
                "supplierName",
                "customerName"
            ],
            route.QueryFields);
    }

    [Theory]
    [InlineData("contract")]
    [InlineData("form")]
    public void LayoutOnlyTypes_UseLayoutWithoutQueryFields(
        string documentType)
    {
        var route =
            DocumentAnalysisRouter.Resolve(
                documentType);

        Assert.Equal(
            DocumentAnalysisModel.Layout,
            route.Model);

        Assert.Null(route.QueryFields);
    }

    [Fact]
    public void UnsupportedType_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                DocumentAnalysisRouter.Resolve(
                    "unknown"));
    }
}
