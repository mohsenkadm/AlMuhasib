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
}
