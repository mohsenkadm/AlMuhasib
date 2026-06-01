using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Services;

public interface IWhatsAppShareService
{
    void ShareInvoice(InvoicePrintModel model, string? customerPhone, string customerName);

    void ShareInstallmentPaymentReceipt(InstallmentPaymentReceiptPrintModel model);
}
