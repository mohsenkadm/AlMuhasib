using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Keeps subtotal, discount, rounding, transport fee, and grand total in sync for invoice screens.
/// </summary>
public static class InvoiceTotalsCalculator
{
    public static (decimal Subtotal, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType,
        AccountingCurrency currency = AccountingCurrency.IQD)
    {
        var result = Compute(lineTotals, invoiceService, invoiceType, invoiceDiscountAmount: 0m, transportFeeAmount: 0m, purchaseExpenseAmount: 0m, currency);
        return (result.Subtotal, result.Rounding, result.GrandTotal);
    }

    public static (decimal Subtotal, decimal InvoiceDiscount, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType,
        decimal invoiceDiscountAmount,
        AccountingCurrency currency = AccountingCurrency.IQD)
        => Compute(lineTotals, invoiceService, invoiceType, invoiceDiscountAmount, transportFeeAmount: 0m, purchaseExpenseAmount: 0m, currency);

    public static (decimal Subtotal, decimal InvoiceDiscount, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType,
        decimal invoiceDiscountAmount,
        decimal transportFeeAmount,
        AccountingCurrency currency = AccountingCurrency.IQD)
        => Compute(lineTotals, invoiceService, invoiceType, invoiceDiscountAmount, transportFeeAmount, purchaseExpenseAmount: 0m, currency);

    public static (decimal Subtotal, decimal InvoiceDiscount, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType,
        decimal invoiceDiscountAmount,
        decimal transportFeeAmount,
        decimal purchaseExpenseAmount,
        AccountingCurrency currency = AccountingCurrency.IQD)
    {
        var sub = AccountingCurrencyHelper.NormalizeAmount(lineTotals.Sum(), currency);
        var discount = AccountingCurrencyHelper.NormalizeAmount(
            Math.Clamp(invoiceDiscountAmount, 0m, Math.Max(0m, sub)), currency);
        var netBeforeRounding = AccountingCurrencyHelper.NormalizeAmount(sub - discount, currency);
        var rounding = invoiceService.CalculateRounding(netBeforeRounding, invoiceType, currency);
        var transport = AccountingCurrencyHelper.NormalizeAmount(Math.Max(0m, transportFeeAmount), currency);
        var purchaseExpense = AccountingCurrencyHelper.NormalizeAmount(Math.Max(0m, purchaseExpenseAmount), currency);
        var grand = AccountingCurrencyHelper.NormalizeAmount(
            netBeforeRounding + rounding + transport + purchaseExpense, currency);
        return (sub, discount, rounding, grand);
    }
}
