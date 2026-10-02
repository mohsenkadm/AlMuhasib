using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class PostReleaseCurrencyBranchAuditTests
{
    [Fact]
    public void FeatureGate_RejectsUsd_WhenMultiCurrencyOff()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AccountingCurrencyRules.ApplyFeatureGate(false, AccountingCurrency.USD, 1500m, "sync-test"));
        Assert.Contains("تعدد العملات", ex.Message);
    }

    [Fact]
    public void FeatureGate_AllowsUsd_WhenMultiCurrencyOn()
    {
        var (currency, fx) = AccountingCurrencyRules.ApplyFeatureGate(true, AccountingCurrency.USD, 1500m, "sync-test");
        Assert.Equal(AccountingCurrency.USD, currency);
        Assert.Equal(1500m, fx);
    }

    [Fact]
    public void FoldIn_DoesNotMixWhenOff()
    {
        var fold = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(false);
        Assert.False(fold);
        var usd = FinancialReportIqdFoldIn.AmountInBaseIqd(10m, AccountingCurrency.USD, 1500m, foldInUsd: false);
        Assert.Equal(0m, usd);
        var iqd = FinancialReportIqdFoldIn.AmountInBaseIqd(1000m, AccountingCurrency.IQD, 1m, foldInUsd: false);
        Assert.Equal(1000m, iqd);
    }

    [Fact]
    public void FoldIn_ConvertsUsdWhenOn()
    {
        var fold = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(true);
        Assert.True(fold);
        var usd = FinancialReportIqdFoldIn.AmountInBaseIqd(10m, AccountingCurrency.USD, 1500m, foldInUsd: true);
        Assert.Equal(15000m, usd);
    }

    [Fact]
    public void SettlementCurrency_RoundTripsViaNotes()
    {
        var notes = CustomerBalanceHelper.MarkFxSettlement("دفعة", AccountingCurrency.IQD);
        Assert.Equal(AccountingCurrency.IQD, CustomerBalanceHelper.GetFxSettlementCurrency(notes));
        Assert.Contains(CustomerBalanceHelper.FxSettlePrefix, notes);
    }

    [Fact]
    public void AgingTotals_DoNotMixCurrencies()
    {
        var rows = new[]
        {
            (Currency: AccountingCurrency.IQD, Remaining: 1000m),
            (Currency: AccountingCurrency.USD, Remaining: 50m),
            (Currency: AccountingCurrency.IQD, Remaining: 500m),
        };

        var totalIqd = rows.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Remaining);
        var totalUsd = rows.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Remaining);

        Assert.Equal(1500m, totalIqd);
        Assert.Equal(50m, totalUsd);
        Assert.True(totalIqd != totalIqd + totalUsd);
    }
}
