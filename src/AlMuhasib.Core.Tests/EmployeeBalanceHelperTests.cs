using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class EmployeeBalanceHelperTests
{
    [Fact]
    public void ComputeAdvanceBalances_SeparatesCurrencies_NoMixing()
    {
        var vouchers = new (AccountingCurrency, VoucherType, decimal)[]
        {
            (AccountingCurrency.IQD, VoucherType.Payment, 100_000m),
            (AccountingCurrency.USD, VoucherType.Payment, 50m),
            (AccountingCurrency.IQD, VoucherType.Receipt, 20_000m),
            (AccountingCurrency.USD, VoucherType.Receipt, 10m),
        };

        var dual = EmployeeBalanceHelper.ComputeAdvanceBalances(
            openingBalance: 5_000m,
            openingCurrency: AccountingCurrency.IQD,
            vouchers);

        Assert.Equal(85_000m, dual.Iqd); // 5000 + 100000 - 20000
        Assert.Equal(40m, dual.Usd);     // 0 + 50 - 10
    }

    [Fact]
    public void ComputeAdvanceBalances_UsdOpening_GoesToUsdOnly()
    {
        var vouchers = new (AccountingCurrency, VoucherType, decimal)[]
        {
            (AccountingCurrency.IQD, VoucherType.Payment, 10_000m),
            (AccountingCurrency.USD, VoucherType.Payment, 5m),
        };

        var dual = EmployeeBalanceHelper.ComputeAdvanceBalances(
            100m, AccountingCurrency.USD, vouchers);

        Assert.Equal(10_000m, dual.Iqd);
        Assert.Equal(105m, dual.Usd);
    }

    [Fact]
    public void ComputeAdvanceBalance_SingleCurrencyFormula()
    {
        Assert.Equal(80m, EmployeeBalanceHelper.ComputeAdvanceBalance(100m, 20m, 40m));
    }
}
