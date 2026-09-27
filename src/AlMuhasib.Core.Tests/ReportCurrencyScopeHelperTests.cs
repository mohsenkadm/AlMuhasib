using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class ReportCurrencyScopeHelperTests
{
    [Theory]
    [InlineData(ReportCurrencyScope.Iqd, true, false)]
    [InlineData(ReportCurrencyScope.Usd, false, true)]
    [InlineData(ReportCurrencyScope.All, true, true)]
    public void Includes_RespectsScope(ReportCurrencyScope scope, bool iqd, bool usd)
    {
        Assert.Equal(iqd, ReportCurrencyScopeHelper.IncludesIqd(scope));
        Assert.Equal(usd, ReportCurrencyScopeHelper.IncludesUsd(scope));
    }

    [Fact]
    public void ToStrictFilter_All_ReturnsNull()
    {
        Assert.Null(ReportCurrencyScopeHelper.ToStrictFilter(ReportCurrencyScope.All));
        Assert.Equal(AccountingCurrency.IQD, ReportCurrencyScopeHelper.ToStrictFilter(ReportCurrencyScope.Iqd));
        Assert.Equal(AccountingCurrency.USD, ReportCurrencyScopeHelper.ToStrictFilter(ReportCurrencyScope.Usd));
    }

    [Fact]
    public void Matches_DoesNotMixWhenStrict()
    {
        Assert.True(ReportCurrencyScopeHelper.Matches(ReportCurrencyScope.Iqd, AccountingCurrency.IQD));
        Assert.False(ReportCurrencyScopeHelper.Matches(ReportCurrencyScope.Iqd, AccountingCurrency.USD));
        Assert.True(ReportCurrencyScopeHelper.Matches(ReportCurrencyScope.All, AccountingCurrency.USD));
    }
}
