using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Infrastructure.Services;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class ProductCostHelperCurrencyTests
{
    [Fact]
    public void ToSignedPurchaseItemInIqd_ConvertsUsdViaSnapshotFxRate()
    {
        var item = new InvoiceItem
        {
            ProductId = 1,
            Quantity = 2,
            UnitPrice = 10m,
            TotalPrice = 20m,
            Invoice = new Invoice
            {
                InvoiceType = InvoiceType.Purchase,
                Currency = AccountingCurrency.USD,
                FxRate = 1500m
            }
        };

        var converted = ProductCostHelper.ToSignedPurchaseItemInIqd(item);

        Assert.Equal(2m, converted.Quantity);
        Assert.Equal(30000m, converted.TotalPrice);
        Assert.Equal(15000m, converted.UnitPrice);
    }

    [Fact]
    public void ToSignedPurchaseItemInIqd_LeavesIqdUnchanged()
    {
        var item = new InvoiceItem
        {
            ProductId = 1,
            Quantity = 3,
            UnitPrice = 1000m,
            TotalPrice = 3000m,
            Invoice = new Invoice
            {
                InvoiceType = InvoiceType.Purchase,
                Currency = AccountingCurrency.IQD,
                FxRate = 1m
            }
        };

        var converted = ProductCostHelper.ToSignedPurchaseItemInIqd(item);

        Assert.Same(item, converted);
        Assert.Equal(3000m, converted.TotalPrice);
    }

    [Fact]
    public void ToSignedPurchaseItemInIqd_NegatesPurchaseReturnAndConvertsUsd()
    {
        var item = new InvoiceItem
        {
            ProductId = 1,
            Quantity = 1,
            UnitPrice = 5m,
            TotalPrice = 5m,
            Invoice = new Invoice
            {
                InvoiceType = InvoiceType.PurchaseReturn,
                Currency = AccountingCurrency.USD,
                FxRate = 1300m
            }
        };

        var converted = ProductCostHelper.ToSignedPurchaseItemInIqd(item);

        Assert.Equal(-1m, converted.Quantity);
        Assert.Equal(-6500m, converted.TotalPrice);
    }
}
