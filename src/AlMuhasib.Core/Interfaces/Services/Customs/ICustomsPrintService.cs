using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

/// <summary>
/// Thin mapper onto <see cref="IExportService"/> — no parallel print stack.
/// Invoice/receipt/statement builds are customer-facing (no internal costs/profit).
/// Report print/export paths enforce <c>CustomsPermissionGuard</c> by screen name.
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

    /// <summary>Print a staff/report table after <c>EnsurePrint(screenName)</c>.</summary>
    void PrintReportTable(
        string screenName,
        string title,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines = null,
        CustomsPrintDocumentKind? kind = null);

    /// <summary>Export report PDF after <c>EnsureExport(screenName)</c>.</summary>
    string ExportReportPdf(
        string screenName,
        StatementPrintModel model,
        CustomsPrintDocumentKind? kind = null);

    /// <summary>Export report Excel after <c>EnsureExport(screenName)</c>.</summary>
    void ExportReportExcel(
        string screenName,
        string filePath,
        string sheetName,
        string[] columns,
        IList<object[]> rows);
}
