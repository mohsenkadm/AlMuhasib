using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class FinancialReportIqdFoldInTests
{
    [Fact]
    public void SumSignedSales_WhenFoldOff_IgnoresUsdEvenIfPresent()
    {
        var items = new (InvoiceType, decimal, AccountingCurrency, decimal)[]
        {
            (InvoiceType.Sale, 100_000m, AccountingCurrency.IQD, 1m),
            (InvoiceType.Sale, 10m, AccountingCurrency.USD, 1500m),
        };

        var total = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(items, foldInUsd: false);
        Assert.Equal(100_000m, total);
    }

    [Fact]
    public void SumSignedSales_WhenFoldOn_AddsUsdConvertedByDocFx()
    {
        var items = new (InvoiceType, decimal, AccountingCurrency, decimal)[]
        {
            (InvoiceType.Sale, 100_000m, AccountingCurrency.IQD, 1m),
            (InvoiceType.Sale, 10m, AccountingCurrency.USD, 1500m),
        };

        var total = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(items, foldInUsd: true);
        Assert.Equal(115_000m, total);
    }

    [Fact]
    public void SumSignedSales_UsdReturn_ReducesSignedSalesAfterConversion()
    {
        var items = new (InvoiceType, decimal, AccountingCurrency, decimal)[]
        {
            (InvoiceType.Sale, 20m, AccountingCurrency.USD, 1500m),
            (InvoiceType.SaleReturn, 5m, AccountingCurrency.USD, 1500m),
        };

        var total = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(items, foldInUsd: true);
        Assert.Equal(22_500m, total); // (20-5)*1500
    }

    [Fact]
    public void SumAmounts_Expenses_FoldUsdByDocFx()
    {
        var items = new (decimal, AccountingCurrency, decimal)[]
        {
            (50_000m, AccountingCurrency.IQD, 1m),
            (2m, AccountingCurrency.USD, 1400m),
        };

        Assert.Equal(50_000m, FinancialReportIqdFoldIn.SumAmountsInBaseIqd(items, foldInUsd: false));
        Assert.Equal(52_800m, FinancialReportIqdFoldIn.SumAmountsInBaseIqd(items, foldInUsd: true));
    }

    [Fact]
    public void CashOrBank_UsesAsOfRateNotDocFx()
    {
        var items = new (decimal, AccountingCurrency)[]
        {
            (1_000_000m, AccountingCurrency.IQD),
            (100m, AccountingCurrency.USD),
        };

        Assert.Equal(1_000_000m,
            FinancialReportIqdFoldIn.SumCashOrBankInBaseIqd(items, asOfUsdToIqd: 1500m, foldInUsd: false));
        Assert.Equal(1_150_000m,
            FinancialReportIqdFoldIn.SumCashOrBankInBaseIqd(items, asOfUsdToIqd: 1500m, foldInUsd: true));
    }

    [Fact]
    public void CashOrBank_MissingAsOfRate_SkipsUsd()
    {
        var total = FinancialReportIqdFoldIn.CashOrBankInBaseIqd(
            100m, AccountingCurrency.USD, asOfUsdToIqd: 0m, foldInUsd: true);
        Assert.Equal(0m, total);
    }

    [Fact]
    public void ShouldFoldUsdIntoIqd_RespectsFeatureAndUsdOnlyScope()
    {
        Assert.False(FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(false));
        Assert.True(FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(true, ReportCurrencyScope.Iqd));
        Assert.True(FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(true, ReportCurrencyScope.All));
        Assert.False(FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(true, ReportCurrencyScope.Usd));
    }
}
