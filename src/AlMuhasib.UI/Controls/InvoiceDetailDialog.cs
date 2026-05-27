using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Controls;

public static class InvoiceDetailDialog
{
    public static void Show(Invoice invoice, string? paymentMethodOverride = null, decimal? companyFeeOverride = null)
    {
        var model = InvoiceDetailMapper.FromInvoice(invoice, paymentMethodOverride, companyFeeOverride);
        var overlay = new InvoiceDetailOverlay { DataContext = model };
        overlay.ShowCentered();
    }
}
