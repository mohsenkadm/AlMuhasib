using AlMuhasib.Core.Interfaces.Services;
using Microsoft.Win32;

namespace AlMuhasib.UI.Services;

public static class ListTableExportHelper
{
    public static void ExportExcel(
        IExportService exportService,
        IToastNotificationService toast,
        bool canExport,
        string filePrefix,
        string sheetName,
        string[] headers,
        IList<object?[]> rows)
    {
        if (!canExport) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;
        exportService.ExportToExcel(dialog.FileName, sheetName, headers, (IList<object[]>)rows);
        toast.ShowSuccess("تم التصدير");
    }

    public static void Print(
        IExportService exportService,
        bool canPrint,
        string title,
        string[] headers,
        IList<object?[]> rows)
    {
        if (!canPrint) return;
        exportService.PrintTable(title, headers, (IList<object[]>)rows);
    }
}
