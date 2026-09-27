using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>حساب سلف الموظفين من الرصيد الافتتاحي والسندات.</summary>
public static class EmployeeBalanceHelper
{
    /// <summary>
    /// رصيد السلفة المستحق على الموظف =
    /// افتتاحي + سندات الدفع − سندات القبض.
    /// </summary>
    public static decimal ComputeAdvanceBalance(
        decimal openingBalance,
        decimal paymentTotal,
        decimal receiptTotal)
        => openingBalance + paymentTotal - receiptTotal;

    public static bool IsEmployeePayment(VoucherType type) => type == VoucherType.Payment;

    public static bool IsEmployeeReceipt(VoucherType type) =>
        type is VoucherType.Receipt or VoucherType.DebtReceipt;
}
