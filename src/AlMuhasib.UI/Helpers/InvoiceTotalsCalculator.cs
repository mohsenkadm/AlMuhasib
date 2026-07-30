using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Keeps subtotal, discount, rounding, and grand total in sync for invoice screens.
/// </summary>
public static class InvoiceTotalsCalculator
{
    public static (decimal Subtotal, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType)
    {
        var result = Compute(lineTotals, invoiceService, invoiceType, invoiceDiscountAmount: 0m);
        return (result.Subtotal, result.Rounding, result.GrandTotal);
    }

    public static (decimal Subtotal, decimal InvoiceDiscount, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType,
        decimal invoiceDiscountAmount)
    {
        var sub = lineTotals.Sum();
        var discount = Math.Clamp(invoiceDiscountAmount, 0m, Math.Max(0m, sub));
        var netBeforeRounding = sub - discount;
        var rounding = invoiceService.CalculateRounding(netBeforeRounding, invoiceType);
        return (sub, discount, rounding, netBeforeRounding + rounding);
    }
}
