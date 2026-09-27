using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>حساب سلف الموظفين من الرصيد الافتتاحي والسندات — بفصل العملات.</summary>
public static class EmployeeBalanceHelper
{
    /// <summary>
    /// رصيد السلفة المستحق على الموظف (عملة واحدة) =
    /// افتتاحي + سندات الدفع − سندات القبض.
    /// </summary>
    public static decimal ComputeAdvanceBalance(
        decimal openingBalance,
        decimal paymentTotal,
        decimal receiptTotal)
        => openingBalance + paymentTotal - receiptTotal;

    /// <summary>
    /// أرصدة سلف مزدوجة — الرصيد الافتتاحي يُحسب ضمن عملته فقط،
    /// والسندات تُجمَّع لكل عملة على حدة (لا خلط IQD+USD).
    /// </summary>
    public static DualCurrencyBalance ComputeAdvanceBalances(
        decimal openingBalance,
        AccountingCurrency openingCurrency,
        IEnumerable<(AccountingCurrency Currency, VoucherType Type, decimal Amount)> vouchers)
    {
        var list = vouchers as IList<(AccountingCurrency Currency, VoucherType Type, decimal Amount)>
                   ?? vouchers.ToList();

        decimal Sum(AccountingCurrency currency, Func<VoucherType, bool> predicate) =>
            list.Where(v => v.Currency == currency && predicate(v.Type)).Sum(v => v.Amount);

        var iqdOpening = openingCurrency == AccountingCurrency.IQD ? openingBalance : 0m;
        var usdOpening = openingCurrency == AccountingCurrency.USD ? openingBalance : 0m;

        var iqd = ComputeAdvanceBalance(
            iqdOpening,
            Sum(AccountingCurrency.IQD, IsEmployeePayment),
            Sum(AccountingCurrency.IQD, IsEmployeeReceipt));

        var usd = ComputeAdvanceBalance(
            usdOpening,
            Sum(AccountingCurrency.USD, IsEmployeePayment),
            Sum(AccountingCurrency.USD, IsEmployeeReceipt));

        return new DualCurrencyBalance(iqd, usd);
    }

    public static bool IsEmployeePayment(VoucherType type) => type == VoucherType.Payment;

    public static bool IsEmployeeReceipt(VoucherType type) =>
        type is VoucherType.Receipt or VoucherType.DebtReceipt;
}
