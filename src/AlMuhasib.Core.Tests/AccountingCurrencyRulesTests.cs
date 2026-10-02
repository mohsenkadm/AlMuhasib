using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class AccountingCurrencyRulesTests
{
    [Fact]
    public void RequireFxRateOrThrow_AllowsIqdWithoutRate()
    {
        var rate = AccountingCurrencyRules.RequireFxRateOrThrow(AccountingCurrency.IQD, 0m);
        Assert.Equal(1m, rate);
    }

    [Fact]
    public void RequireFxRateOrThrow_RejectsUsdWithoutRate()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AccountingCurrencyRules.RequireFxRateOrThrow(AccountingCurrency.USD, 0m, "اختبار"));
    }

    [Fact]
    public void EnsureSameCurrency_ThrowsOnMismatch()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AccountingCurrencyRules.EnsureSameCurrency(
                AccountingCurrency.IQD, AccountingCurrency.USD, "فاتورة", "قاصة"));
    }

    [Theory]
    [InlineData(AccountingCurrency.USD)]
    [InlineData(1)]
    [InlineData((byte)1)]
    [InlineData("USD")]
    [InlineData("$")]
    public void TryResolve_AcceptsUsdVariants(object value)
    {
        Assert.True(AccountingCurrencyHelper.TryResolve(value, out var currency));
        Assert.Equal(AccountingCurrency.USD, currency);
    }

    [Theory]
    [InlineData(AccountingCurrency.IQD)]
    [InlineData(0)]
    [InlineData("IQD")]
    [InlineData("د.ع")]
    public void TryResolve_AcceptsIqdVariants(object value)
    {
        Assert.True(AccountingCurrencyHelper.TryResolve(value, out var currency));
        Assert.Equal(AccountingCurrency.IQD, currency);
    }

    [Fact]
    public void ToBaseIqdStrict_ConvertsUsdWithStoredRate()
    {
        var iqd = AccountingCurrencyRules.ToBaseIqdStrict(10m, AccountingCurrency.USD, 1500m);
        Assert.Equal(15000m, iqd);
    }

    [Fact]
    public void ToBaseIqdStrict_RejectsBadUsdRate()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AccountingCurrencyRules.ToBaseIqdStrict(10m, AccountingCurrency.USD, 0m));
    }

    [Fact]
    public void ApplyFeatureGate_WhenOff_RejectsUsd()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AccountingCurrencyRules.ApplyFeatureGate(false, AccountingCurrency.USD, 1500m, "API"));
    }

    [Fact]
    public void ApplyFeatureGate_WhenOff_ForcesIqdFxRateOne()
    {
        var (currency, fx) = AccountingCurrencyRules.ApplyFeatureGate(
            false, AccountingCurrency.IQD, 999m, "اختبار");
        Assert.Equal(AccountingCurrency.IQD, currency);
        Assert.Equal(1m, fx);
    }

    [Fact]
    public void ApplyFeatureGate_WhenOn_AllowsUsdWithValidRate()
    {
        var (currency, fx) = AccountingCurrencyRules.ApplyFeatureGate(
            true, AccountingCurrency.USD, 1310m, "اختبار");
        Assert.Equal(AccountingCurrency.USD, currency);
        Assert.Equal(1310m, fx);
    }

    [Fact]
    public void ApplyFeatureGate_WhenOn_RejectsUsdWithoutRate()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AccountingCurrencyRules.ApplyFeatureGate(true, AccountingCurrency.USD, 0m, "اختبار"));
    }
}
