using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

/// <summary>قواعد رفض FxRate الصامت — نفس مبدأ مسار الذهب بعد الإصلاح.</summary>
public class CurrencyFxIntegrityTests
{
    [Fact]
    public void RequireFxRateOrThrow_RejectsZeroUsdRate()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AccountingCurrencyRules.RequireFxRateOrThrow(AccountingCurrency.USD, 0m, "ذهب"));
    }

    [Fact]
    public void ToBaseIqdStrict_UsesDocumentStoredRate()
    {
        var a = AccountingCurrencyRules.ToBaseIqdStrict(10m, AccountingCurrency.USD, 1400m);
        var b = AccountingCurrencyRules.ToBaseIqdStrict(10m, AccountingCurrency.USD, 1500m);
        Assert.Equal(14000m, a);
        Assert.Equal(15000m, b);
        Assert.NotEqual(a, b);
    }
}
