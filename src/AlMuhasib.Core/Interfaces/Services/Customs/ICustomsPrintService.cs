using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

/// <summary>
/// Thin mapper onto <see cref="IExportService"/> — no parallel print stack.
/// Invoice/receipt/statement builds are customer-facing (no internal costs/profit).
/// </summary>
public interface ICustomsPrintService
{
    CustomsPrintDocumentKind ResolveInvoiceKind(CustomsServiceType serviceType);

    InvoicePrintModel BuildCustomerInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null);

    void PrintCustomerInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null);

    string ExportCustomerInvoicePdf(
        CustomsInvoiceDetail detail,
        string? customerPhone = null);

    VoucherPrintModel BuildReceipt(CustomsReceiptDetail detail);

    string ExportReceiptPdf(CustomsReceiptDetail detail);

    StatementPrintModel BuildCustomerStatement(
        string customerName,
        string? customerPhone,
        DateTime? fromDate,
        DateTime? toDate,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines);

    void PrintCustomerStatement(StatementPrintModel model);

    string ExportCustomerStatementPdf(StatementPrintModel model);

    void PrintReportTable(
        CustomsPrintDocumentKind kind,
        string title,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines = null);

    string ExportReportPdf(
        CustomsPrintDocumentKind kind,
        StatementPrintModel model);
}
