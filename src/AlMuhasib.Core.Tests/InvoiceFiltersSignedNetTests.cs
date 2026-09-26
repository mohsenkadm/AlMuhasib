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

    [Fact]
    public void ForProfitAndSalesTotals_DefaultsToIqd_AndSeparatesUsd()
    {
        var invoices = new List<Invoice>
        {
            new() { Id = 1, InvoiceType = InvoiceType.Sale, Currency = AccountingCurrency.IQD, NetAmount = 100 },
            new() { Id = 2, InvoiceType = InvoiceType.Sale, Currency = AccountingCurrency.USD, NetAmount = 10 },
            new() { Id = 3, InvoiceType = InvoiceType.SaleReturn, Currency = AccountingCurrency.IQD, NetAmount = 20 },
        }.AsQueryable();
        var plans = new List<InstallmentPlan>().AsQueryable();

        var iqd = InvoiceFilters.ForProfitAndSalesTotals(invoices, plans).ToList();
        var usd = InvoiceFilters.ForProfitAndSalesTotals(invoices, plans, AccountingCurrency.USD).ToList();

        Assert.Equal(2, iqd.Count);
        Assert.All(iqd, i => Assert.Equal(AccountingCurrency.IQD, i.Currency));
        Assert.Single(usd);
        Assert.Equal(AccountingCurrency.USD, usd[0].Currency);
    }

    [Fact]
    public void ForPurchasesTotals_SeparatesByCurrency()
    {
        var invoices = new List<Invoice>
        {
            new() { Id = 1, InvoiceType = InvoiceType.Purchase, Currency = AccountingCurrency.IQD, NetAmount = 50 },
            new() { Id = 2, InvoiceType = InvoiceType.Purchase, Currency = AccountingCurrency.USD, NetAmount = 5 },
        }.AsQueryable();

        Assert.Single(InvoiceFilters.ForPurchasesTotals(invoices).ToList());
        Assert.Single(InvoiceFilters.ForPurchasesTotals(invoices, AccountingCurrency.USD).ToList());
    }
}
