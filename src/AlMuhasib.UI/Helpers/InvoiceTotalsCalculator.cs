using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Keeps subtotal, rounding, and grand total in sync for invoice screens.
/// </summary>
public static class InvoiceTotalsCalculator
{
    public static (decimal Subtotal, decimal Rounding, decimal GrandTotal) Compute(
        IEnumerable<decimal> lineTotals,
        IInvoiceService invoiceService,
        InvoiceType invoiceType)
    {
        var sub = lineTotals.Sum();
        var rounding = invoiceService.CalculateRounding(sub, invoiceType);
        return (sub, rounding, sub + rounding);
    }
}
