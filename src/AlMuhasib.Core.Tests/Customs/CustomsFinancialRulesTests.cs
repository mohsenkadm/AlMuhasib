using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsFinancialRulesTests
{
    [Theory]
    [InlineData(1000, 0, 1000)]
    [InlineData(1000, 400, 600)]
    [InlineData(1000, 1000, 0)]
    [InlineData(1000, 1200, 0)]
    public void ComputeRemaining_clamps_at_zero(decimal total, decimal paid, decimal expected) =>
        Assert.Equal(expected, CustomsFinancialRules.ComputeRemaining(total, paid));

    [Fact]
    public void ComputeInternalProfit_iqd_only_subtracts_same_currency_expenses()
    {
        var profit = CustomsFinancialRules.ComputeInternalProfit(
            invoiceTotal: 1000m,
            invoiceCurrency: AccountingCurrency.IQD,
            expenses:
            [
                (AccountingCurrency.IQD, 200m),
                (AccountingCurrency.IQD, 50m)
            ]);

        Assert.Equal(750m, profit);
    }

    [Fact]
    public void ComputeInternalProfit_usd_only()
    {
        var profit = CustomsFinancialRules.ComputeInternalProfit(
            500m,
            AccountingCurrency.USD,
            [(AccountingCurrency.USD, 80m)]);

        Assert.Equal(420m, profit);
    }

    [Fact]
    public void ComputeInternalProfit_excludes_other_currency_expenses()
    {
        var profit = CustomsFinancialRules.ComputeInternalProfit(
            invoiceTotal: 1000m,
            invoiceCurrency: AccountingCurrency.IQD,
            expenses:
            [
                (AccountingCurrency.IQD, 100m),
                (AccountingCurrency.USD, 999m) // must not fold into IQD profit
            ],
            out var sameCurrencySum);

        Assert.Equal(100m, sameCurrencySum);
        Assert.Equal(900m, profit);
    }

    [Fact]
    public void EnsureVehicleCurrencyMatchesInvoice_throws_on_mix()
    {
        Assert.ThrowsAny<Exception>(() =>
            CustomsFinancialRules.EnsureVehicleCurrencyMatchesInvoice(
                AccountingCurrency.USD,
                AccountingCurrency.IQD));
    }

    [Fact]
    public void EnsureReceiptMatchesInvoiceCurrency_throws_on_mix()
    {
        Assert.ThrowsAny<Exception>(() =>
            CustomsFinancialRules.EnsureReceiptMatchesInvoiceCurrency(
                AccountingCurrency.IQD,
                AccountingCurrency.USD));
    }

    [Fact]
    public void AssertSingleCurrencyChartAmount_throws_when_mixed()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CustomsFinancialRules.AssertSingleCurrencyChartAmount("مبيعات", mixesCurrencies: true));
    }

    [Fact]
    public void AssertSingleCurrencyChartAmount_allows_single_currency()
    {
        CustomsFinancialRules.AssertSingleCurrencyChartAmount("مبيعات", mixesCurrencies: false);
    }

    [Fact]
    public void NormalizeFxRate_usd_rejects_non_positive()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CustomsFinancialRules.NormalizeFxRate(AccountingCurrency.USD, 0m, "اختبار"));
    }

    [Fact]
    public void NormalizeFxRate_iqd_is_one() =>
        Assert.Equal(1m, CustomsFinancialRules.NormalizeFxRate(AccountingCurrency.IQD, 99m));
}
