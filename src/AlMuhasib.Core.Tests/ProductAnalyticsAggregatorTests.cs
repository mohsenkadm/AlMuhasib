using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class ProductAnalyticsAggregatorTests
{
    [Fact]
    public void NetSales_SubtractReturnsAndHeaderDiscount_Once()
    {
        var product = new ProductAnalyticsProduct { Id = 7, Name = "صنف", CategoryName = "عام", UnitName = "قطعة" };
        var lines = new List<ProductAnalyticsSourceLine>
        {
            Line(1, InvoiceType.Sale, 10, 1000, 10_000, headerDiscount: 1_000, invoiceLinesTotal: 10_000),
            Line(2, InvoiceType.SaleReturn, 2, 1000, 2_000, headerDiscount: 0, invoiceLinesTotal: 2_000),
            Line(3, InvoiceType.Purchase, 10, 400, 4_000, headerDiscount: 0, invoiceLinesTotal: 4_000)
        };

        var result = ProductAnalyticsAggregator.Build(
            new ProductAnalyticsQuery { From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 31) },
            [product],
            lines,
            new Dictionary<int, decimal> { [7] = 400m },
            [new ProductAnalyticsStock { ProductId = 7, WarehouseId = 1, Quantity = 8, MinQuantity = 2 }],
            foldUsd: false,
            multiCurrencyEnabled: false);

        var row = Assert.Single(result.Rows);
        Assert.Equal(10m, row.SoldQuantity);
        Assert.Equal(2m, row.ReturnQuantity);
        Assert.Equal(8m, row.NetQuantitySold);
        Assert.Equal(7_000m, row.SalesAmount);
        Assert.Equal(3_200m, row.Cogs);
        Assert.Equal(3_800m, row.GrossProfit);
        Assert.Equal(7_000m, result.NetSales);
        Assert.Equal(3_800m, result.GrossProfit);
        Assert.Equal(4_000m, result.PurchaseValue);
        Assert.Equal(2_000m, result.ReturnValue);
        var category = Assert.Single(result.Categories);
        Assert.Equal("عام", category.Name);
        Assert.Equal(7_000m, category.SalesAmount);
        Assert.Equal(3_800m, category.GrossProfit);
        Assert.Equal(1, category.ProductCount);
    }

    [Fact]
    public void UsdLines_AreExcluded_WhenMultiCurrencyIsOff()
    {
        var product = new ProductAnalyticsProduct { Id = 7, Name = "دولار", CategoryName = "عام" };
        var lines = new List<ProductAnalyticsSourceLine>
        {
            Line(1, InvoiceType.Sale, 1, 10, 10, 0, 10, AccountingCurrency.USD)
        };

        var result = ProductAnalyticsAggregator.Build(
            new ProductAnalyticsQuery(),
            [product],
            lines,
            new Dictionary<int, decimal>(),
            [],
            foldUsd: false,
            multiCurrencyEnabled: false);

        var row = Assert.Single(result.Rows);
        Assert.Equal(0m, row.SalesAmount);
        Assert.Equal(0m, row.SoldQuantity);
    }

    private static ProductAnalyticsSourceLine Line(
        int invoiceId,
        InvoiceType type,
        decimal quantity,
        decimal unitPrice,
        decimal lineTotal,
        decimal headerDiscount,
        decimal invoiceLinesTotal,
        AccountingCurrency currency = AccountingCurrency.IQD) => new()
    {
        ProductId = 7,
        InvoiceId = invoiceId,
        InvoiceNumber = invoiceId.ToString(),
        Type = type,
        Date = new DateTime(2026, 1, 15),
        Quantity = quantity,
        UnitPrice = unitPrice,
        LineTotal = lineTotal,
        HeaderDiscount = headerDiscount,
        InvoiceLinesTotal = invoiceLinesTotal,
        Currency = currency,
        FxRate = currency == AccountingCurrency.USD ? 1500 : 1,
        WarehouseId = 1,
        WarehouseName = "الرئيسي"
    };
}
