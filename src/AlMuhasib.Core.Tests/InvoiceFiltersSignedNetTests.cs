using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Models;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class InvoiceFiltersSignedNetTests
{
    [Fact]
    public void SignedNetAmount_Sale_IsPositive()
    {
        Assert.Equal(100m, InvoiceFilters.SignedNetAmount(InvoiceType.Sale, 100m));
    }

    [Fact]
    public void SignedNetAmount_SaleReturn_IsNegative()
    {
        Assert.Equal(-40m, InvoiceFilters.SignedNetAmount(InvoiceType.SaleReturn, 40m));
    }

    [Fact]
    public void SignedNetAmount_PurchaseReturn_IsNegative()
    {
        Assert.Equal(-25m, InvoiceFilters.SignedNetAmount(InvoiceType.PurchaseReturn, 25m));
    }

    [Fact]
    public void SumSignedNet_NetsSalesMinusReturns()
    {
        var invoices = new List<Invoice>
        {
            new() { InvoiceType = InvoiceType.Sale, NetAmount = 100 },
            new() { InvoiceType = InvoiceType.Installment, NetAmount = 50 },
            new() { InvoiceType = InvoiceType.SaleReturn, NetAmount = 30 },
        };

        Assert.Equal(120m, InvoiceFilters.SumSignedNet(invoices));
    }

    [Fact]
    public void IsReturnType_OnlyReturns()
    {
        Assert.True(InvoiceFilters.IsReturnType(InvoiceType.SaleReturn));
        Assert.True(InvoiceFilters.IsReturnType(InvoiceType.PurchaseReturn));
        Assert.False(InvoiceFilters.IsReturnType(InvoiceType.Sale));
        Assert.False(InvoiceFilters.IsReturnType(InvoiceType.Purchase));
    }
}
