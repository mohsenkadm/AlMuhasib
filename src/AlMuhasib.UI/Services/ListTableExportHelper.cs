using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using Microsoft.Win32;

namespace AlMuhasib.UI.Services;

public static class ListTableExportHelper
{
    public static void ExportExcel(
        IExportService exportService,
        IToastNotificationService? toast,
        bool canExport,
        string filePrefix,
        string sheetName,
        string[] headers,
        IList<object[]> rows)
    {
        if (!canExport)
        {
            if (toast is null)
                BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية التصدير");
            return;
        }

        var fileName = filePrefix.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? filePrefix
            : $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = fileName,
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        exportService.ExportToExcel(dialog.FileName, sheetName, headers, rows);

        if (toast is not null)
            toast.ShowSuccess("تم التصدير");
        else
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    public static void Print(
        IExportService exportService,
        bool canPrint,
        string title,
        string[] headers,
        IList<object[]> rows,
        IList<string>? summaryLines = null,
        bool warnOnDenied = false)
    {
        if (!canPrint)
        {
            if (warnOnDenied)
                BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية الطباعة");
            return;
        }

        exportService.PrintTable(title, headers, rows, summaryLines);
    }

    /// <summary>Convert nullable cell rows (Hotel) to non-null cell arrays for export/print.</summary>
    public static IList<object[]> ToRows(IEnumerable<object?[]> rows) =>
        rows.Select(r => r.Select(c => (object)(c ?? string.Empty)).ToArray()).ToList();
}
