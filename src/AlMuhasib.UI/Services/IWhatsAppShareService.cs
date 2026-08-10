using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Services;

public interface IWhatsAppShareService
{
    /// <summary>مشاركة ملف PDF عام عبر واتساب بعد تطبيع الرقم العراقي.</summary>
    void SharePdf(string? phone, string partyName, string pdfPath, string message, string title);

    /// <summary>فتح محادثة واتساب برسالة نصية فقط (بدون PDF).</summary>
    void ShareTextMessage(string? phone, string partyName, string message);

    void ShareInvoice(InvoicePrintModel model, string? customerPhone, string customerName);

    void ShareInstallmentPaymentReceipt(InstallmentPaymentReceiptPrintModel model);

    void ShareVoucher(VoucherPrintModel model, string? partyPhone, string partyName);

    void ShareInvestorTransaction(InvestorTransactionPrintModel model);

    void ShareStatement(StatementPrintModel model, string? partyPhone, string partyName);
}
