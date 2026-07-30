using AlMuhasib.Core.Enums;
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
        InvoiceType invoiceType)
    {
        var result = Compute(lineTotals, invoiceService, invoiceType, invoiceDiscountAmount: 0m, transportFeeAmount: 0m);
        return (result.Subtotal, result.Rounding, result.GrandTotal);
    }

    public static (decimal Subtotal, decimal InvoiceDiscount, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType,
        decimal invoiceDiscountAmount)
        => Compute(lineTotals, invoiceService, invoiceType, invoiceDiscountAmount, transportFeeAmount: 0m);

    public static (decimal Subtotal, decimal InvoiceDiscount, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType,
        decimal invoiceDiscountAmount,
        decimal transportFeeAmount)
    {
        var sub = lineTotals.Sum();
        var discount = Math.Clamp(invoiceDiscountAmount, 0m, Math.Max(0m, sub));
        var netBeforeRounding = sub - discount;
        var rounding = invoiceService.CalculateRounding(netBeforeRounding, invoiceType);
        var transport = Math.Max(0m, transportFeeAmount);
        return (sub, discount, rounding, netBeforeRounding + rounding + transport);
    }
}
