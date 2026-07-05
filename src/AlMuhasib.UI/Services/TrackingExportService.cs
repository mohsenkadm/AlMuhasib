using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Shared.Services;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Wraps Excel export operations and records saved file paths for Open Recent.
/// </summary>
public sealed class TrackingExportService : IExportService
{
    private readonly ExcelExportService _inner;
    private readonly IRecentExcelExportService _recentExcel;

    public TrackingExportService(ExcelExportService inner, IRecentExcelExportService recentExcel)
    {
        _inner = inner;
        _recentExcel = recentExcel;
    }

    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1") =>
        _inner.ExportToExcel(data, sheetName);

    public async Task ExportToExcelFileAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1")
    {
        await _inner.ExportToExcelFileAsync(data, filePath, sheetName);
        _recentExcel.RecordExport(filePath, sheetName);
    }

    public void ExportToExcel(string filePath, string sheetName, string[] columns, IList<object[]> rows)
    {
        _inner.ExportToExcel(filePath, sheetName, columns, rows);
        _recentExcel.RecordExport(filePath, sheetName);
    }

    public void PrintTable(string title, string[] columns, IList<object[]> rows, IList<string>? summaryLines = null) =>
        _inner.PrintTable(title, columns, rows, summaryLines);

    public void PrintInvoice(InvoicePrintModel model) =>
        _inner.PrintInvoice(model);

    public string ExportInvoiceToPdf(InvoicePrintModel model) =>
        _inner.ExportInvoiceToPdf(model);

    public string ExportInstallmentPaymentReceiptToPdf(InstallmentPaymentReceiptPrintModel model) =>
        _inner.ExportInstallmentPaymentReceiptToPdf(model);

    public void PrintThermalReceipt(InvoicePrintModel model) =>
        _inner.PrintThermalReceipt(model);

    public string ExportInstallmentContractToPdf(InvoicePrintModel model) =>
        _inner.ExportInstallmentContractToPdf(model);

    public void PrintInstallmentSchedule(InvoicePrintModel model) =>
        _inner.PrintInstallmentSchedule(model);

    public void PrintInstallmentPlanDetail(InstallmentPlanDetailPrintModel model) =>
        _inner.PrintInstallmentPlanDetail(model);

    public void PrintInstallmentMultiPlanDetail(IReadOnlyList<InstallmentPlanDetailPrintModel> plans, string title) =>
        _inner.PrintInstallmentMultiPlanDetail(plans, title);

    public void PrintInstallmentPlansSummary(InstallmentPlansSummaryPrintModel model) =>
        _inner.PrintInstallmentPlansSummary(model);
}
