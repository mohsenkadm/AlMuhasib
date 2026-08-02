using System.Diagnostics;
using System.Windows;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Shared.Helpers;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Windows;

namespace AlMuhasib.UI.Services;

public sealed class WhatsAppShareService : IWhatsAppShareService
{
    private readonly IExportService _exportService;

    public WhatsAppShareService(IExportService exportService)
    {
        _exportService = exportService;
    }

    public void SharePdf(string? phone, string partyName, string pdfPath, string message, string title)
    {
        if (!TryResolvePhone(phone, partyName, out var waDigits, out var displayPhone))
            return;

        OpenWhatsAppWithPdf(waDigits, displayPhone, pdfPath, message, title);
    }

    public void ShareInvoice(InvoicePrintModel model, string? customerPhone, string customerName)
    {
        var phone = !string.IsNullOrWhiteSpace(customerPhone) ? customerPhone : model.PartyPhone;
        if (!TryResolvePhone(phone, customerName, out var waDigits, out var displayPhone))
            return;

        var pdfPath = _exportService.ExportInvoiceToPdf(model);
        var message = BuildInvoiceMessage(model, customerName);
        OpenWhatsAppWithPdf(waDigits, displayPhone, pdfPath, message, "إرسال الفاتورة عبر واتساب");
    }

    public void ShareInstallmentPaymentReceipt(InstallmentPaymentReceiptPrintModel model)
    {
        if (!TryResolvePhone(model.CustomerPhone, model.CustomerName, out var waDigits, out var displayPhone))
            return;

        var pdfPath = _exportService.ExportInstallmentPaymentReceiptToPdf(model);
        var message = BuildPaymentMessage(model);
        OpenWhatsAppWithPdf(waDigits, displayPhone, pdfPath, message, "إرسال إيصال التسديد عبر واتساب");
    }

    public void ShareVoucher(VoucherPrintModel model, string? partyPhone, string partyName)
    {
        var phone = !string.IsNullOrWhiteSpace(partyPhone) ? partyPhone : model.PartyPhone;
        var name = !string.IsNullOrWhiteSpace(partyName) ? partyName : (model.PartyName ?? "الطرف");
        if (!TryResolvePhone(phone, name, out var waDigits, out var displayPhone))
            return;

        var pdfPath = _exportService.ExportVoucherToPdf(model);
        var message = BuildVoucherMessage(model, name);
        OpenWhatsAppWithPdf(waDigits, displayPhone, pdfPath, message, "إرسال السند عبر واتساب");
    }

    public void ShareInvestorTransaction(InvestorTransactionPrintModel model)
    {
        if (!TryResolvePhone(model.InvestorPhone, model.InvestorName, out var waDigits, out var displayPhone))
            return;

        var pdfPath = _exportService.ExportInvestorTransactionToPdf(model);
        var message = BuildInvestorMessage(model);
        OpenWhatsAppWithPdf(waDigits, displayPhone, pdfPath, message, "إرسال إيصال المستثمر عبر واتساب");
    }

    public void ShareStatement(StatementPrintModel model, string? partyPhone, string partyName)
    {
        var phone = !string.IsNullOrWhiteSpace(partyPhone) ? partyPhone : model.PartyPhone;
        var name = !string.IsNullOrWhiteSpace(partyName) ? partyName : model.PartyName;
        if (!TryResolvePhone(phone, name, out var waDigits, out var displayPhone))
            return;

        var pdfPath = _exportService.ExportStatementToPdf(model);
        var message = BuildStatementMessage(model, name);
        OpenWhatsAppWithPdf(waDigits, displayPhone, pdfPath, message, "إرسال الكشف عبر واتساب");
    }

    private static bool TryResolvePhone(string? customerPhone, string customerName, out string waDigits, out string displayPhone)
    {
        waDigits = string.Empty;
        displayPhone = string.Empty;

        var phone = customerPhone?.Trim();
        if (string.IsNullOrWhiteSpace(phone) ||
            !IraqiPhoneHelper.TryNormalizeForWhatsApp(phone, out waDigits, out displayPhone, out _))
        {
            var dialog = new WhatsAppPhoneDialog(customerName, phone)
            {
                Owner = GetOwnerWindow()
            };
            if (dialog.ShowDialog() != true)
                return false;

            if (!IraqiPhoneHelper.TryNormalizeForWhatsApp(dialog.PhoneNumber, out waDigits, out displayPhone, out var err))
            {
                BeautifulMessageDialog.ShowError(err ?? "رقم الهاتف غير صالح.");
                return false;
            }
        }

        return true;
    }

    private void OpenWhatsAppWithPdf(string waDigits, string displayPhone, string pdfPath, string message, string shareTitle)
    {
        TryOpenWhatsAppChat(waDigits, message);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{pdfPath}\"",
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore
        }

        CopyPdfToClipboard(pdfPath);

        BeautifulMessageDialog.ShowInfo(
            $"تم تجهيز الإرسال إلى {displayPhone}.\n\n" +
            $"• فُتح واتساب مع نص الرسالة جاهزاً.\n" +
            $"• ملف PDF: {pdfPath}\n" +
            "• أرفق الملف من نافذة المستكشف (زر 📎 ثم اختر الملف، أو اسحبه إلى المحادثة).\n\n" +
            "بعد الإرفاق اضغط «إرسال» فقط.",
            shareTitle);
    }

    private static void CopyPdfToClipboard(string pdfPath)
    {
        try
        {
            var files = new System.Collections.Specialized.StringCollection { pdfPath };
            Clipboard.SetFileDropList(files);
        }
        catch
        {
            // ignore clipboard failures
        }
    }

    private static void TryOpenWhatsAppChat(string waDigits, string message)
    {
        var encoded = Uri.EscapeDataString(message);
        var urls = new[]
        {
            $"whatsapp://send?phone={waDigits}&text={encoded}",
            $"https://wa.me/{waDigits}?text={encoded}"
        };

        foreach (var url in urls)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                return;
            }
            catch
            {
                // try next
            }
        }
    }

    private static string BuildInvoiceMessage(InvoicePrintModel m, string customerName)
    {
        var partyLabel = string.IsNullOrWhiteSpace(m.PartyLabel) ? "العميل" : m.PartyLabel;
        var lines = new List<string>
        {
            "السلام عليكم،",
            $"مرفق {m.Title}.",
            $"رقم الفاتورة: {m.InvoiceNumber}",
            $"التاريخ: {m.Date:yyyy/MM/dd}",
            $"{partyLabel}: {customerName}",
            $"الإجمالي: {m.GrandTotal:N0} د.ع"
        };
        if (!string.IsNullOrWhiteSpace(m.Notes))
            lines.Add($"ملاحظات: {m.Notes}");
        lines.Add("");
        lines.Add("مع التحية — المحاسب");
        return string.Join("\n", lines);
    }

    private static string BuildPaymentMessage(InstallmentPaymentReceiptPrintModel m)
    {
        var lines = new List<string>
        {
            "السلام عليكم،",
            "مرفق إيصال تسديد قسط/أقساط.",
            $"العميل: {m.CustomerName}",
            $"فاتورة الأقساط: {m.InvoiceNumber}",
            $"تاريخ التسديد: {m.PaymentDate:yyyy/MM/dd}",
            $"المبلغ المسدّد: {m.TotalPaid:N0} د.ع"
        };
        if (m.PlanRemainingTotal.HasValue)
            lines.Add($"المتبقي على الخطة: {m.PlanRemainingTotal:N0} د.ع");
        if (!string.IsNullOrWhiteSpace(m.Notes))
            lines.Add($"ملاحظات: {m.Notes}");
        lines.Add("");
        lines.Add("مع التحية — المحاسب");
        return string.Join("\n", lines);
    }

    private static string BuildVoucherMessage(VoucherPrintModel m, string partyName)
    {
        var lines = new List<string>
        {
            "السلام عليكم،",
            $"مرفق {m.Title}.",
            $"رقم السند: {m.VoucherNumber}",
            $"النوع: {m.VoucherTypeLabel}",
            $"التاريخ: {m.Date:yyyy/MM/dd}",
            $"الطرف: {partyName}",
            $"المبلغ: {m.Amount:N0} د.ع"
        };
        if (m.BankFees > 0)
            lines.Add($"عمولة المصرف: {m.BankFees:N0} د.ع");
        if (!string.IsNullOrWhiteSpace(m.Notes))
            lines.Add($"ملاحظات: {m.Notes}");
        lines.Add("");
        lines.Add("مع التحية — المحاسب");
        return string.Join("\n", lines);
    }

    private static string BuildInvestorMessage(InvestorTransactionPrintModel m)
    {
        var lines = new List<string>
        {
            "السلام عليكم،",
            $"مرفق {m.Title}.",
            $"المستثمر: {m.InvestorName}",
            $"النوع: {m.TransactionTypeLabel}",
            $"التاريخ: {m.Date:yyyy/MM/dd}",
            $"المبلغ: {m.Amount:N0} د.ع"
        };
        if (m.BalanceAfter.HasValue)
            lines.Add($"الرصيد بعد العملية: {m.BalanceAfter.Value:N0} د.ع");
        if (!string.IsNullOrWhiteSpace(m.Notes))
            lines.Add($"ملاحظات: {m.Notes}");
        lines.Add("");
        lines.Add("مع التحية — المحاسب");
        return string.Join("\n", lines);
    }

    private static string BuildStatementMessage(StatementPrintModel m, string partyName)
    {
        var lines = new List<string>
        {
            "السلام عليكم،",
            $"مرفق {m.Title}.",
            $"الاسم: {partyName}"
        };
        if (m.FromDate.HasValue || m.ToDate.HasValue)
        {
            var from = m.FromDate?.ToString("yyyy/MM/dd") ?? "—";
            var to = m.ToDate?.ToString("yyyy/MM/dd") ?? "—";
            lines.Add($"الفترة: {from} → {to}");
        }

        if (m.SummaryLines is { Count: > 0 })
        {
            foreach (var summary in m.SummaryLines.Take(3))
                lines.Add(summary);
        }

        lines.Add("");
        lines.Add("مع التحية — المحاسب");
        return string.Join("\n", lines);
    }

    private static Window? GetOwnerWindow() =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? Application.Current.MainWindow;
}
