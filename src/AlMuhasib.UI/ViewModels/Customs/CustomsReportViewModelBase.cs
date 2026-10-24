using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Customs;

public abstract partial class CustomsReportViewModelBase : ViewModelBase
{
    protected readonly ICustomsReportService ReportService;
    protected readonly ICustomsPrintService PrintService;
    protected readonly IExportService ExportService;
    protected readonly ICurrentUserService CurrentUserService;

    /// <summary>Screen used for Print/Export service-layer guards (Reports or ProfitReport).</summary>
    protected string PermissionScreen { get; set; } = CustomsScreenNames.Reports;

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalRecords;
    [ObservableProperty] private int _pageSize = 25;
    [ObservableProperty] private string _paginationText = string.Empty;

    public ObservableCollection<CustomsReportCustomerOption> Customers { get; } = [];

    protected CustomsReportViewModelBase(
        ICustomsReportService reportService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        ReportService = reportService;
        PrintService = printService;
        ExportService = exportService;
        CurrentUserService = currentUserService;
    }

    protected void UpdatePaginationWithFilters<T>(IList<T> allItems, ObservableCollection<T> displayItems)
    {
        var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
        PaginationHelper.ApplyPage(filtered, displayItems, CurrentPage, PageSize,
            out var totalRecords, out var totalPages, out var paginationText);

        TotalRecords = totalRecords;
        TotalPages = totalPages;
        PaginationText = paginationText;

        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        OnPageChanged();
    }

    protected abstract void OnPageChanged();

    [RelayCommand]
    protected void FirstPage() { CurrentPage = 1; OnPageChanged(); }

    [RelayCommand]
    protected void PreviousPage() { if (CurrentPage > 1) { CurrentPage--; OnPageChanged(); } }

    [RelayCommand]
    protected void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; OnPageChanged(); } }

    [RelayCommand]
    protected void LastPage() { CurrentPage = TotalPages; OnPageChanged(); }

    [RelayCommand]
    protected void SetDateToday() => DateFrom = DateTo = DateTime.Today;

    [RelayCommand]
    protected void SetDateWeek()
    {
        DateTo = DateTime.Today;
        DateFrom = DateTime.Today.AddDays(-7);
    }

    [RelayCommand]
    protected void SetDateMonth()
    {
        DateTo = DateTime.Today;
        DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    [RelayCommand]
    protected void ResetDateFilters()
    {
        DateFrom = DateTime.Today.AddMonths(-1);
        DateTo = DateTime.Today;
    }

    protected async Task LoadCustomerOptionsAsync()
    {
        Customers.Clear();
        Customers.Add(new CustomsReportCustomerOption { Id = 0, Name = "الكل" });
        foreach (var c in await ReportService.GetCustomerOptionsAsync())
            Customers.Add(c);
    }

    protected static string FormatCurrency(decimal value, AccountingCurrency currency = AccountingCurrency.IQD)
        => AccountingCurrencyHelper.Format(value, currency);

    protected void ExportExcel(string fileName, string sheetTitle, string[] columns, IList<object[]> rows)
    {
        if (!CanExport)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية التصدير");
            return;
        }

        var resolved = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = resolved,
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            PrintService.ExportReportExcel(PermissionScreen, dialog.FileName, sheetTitle, columns, rows);
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    protected void PrintTable(string title, string[] columns, IList<object[]> rows, IList<string>? summaryLines = null)
        => PrintStaffReport(null, title, columns, rows, summaryLines);

    /// <summary>Staff operational report print via document-kind key (full internal data allowed).</summary>
    protected void PrintStaffReport(
        CustomsPrintDocumentKind? kind,
        string title,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines = null,
        string? partyName = null)
    {
        if (!CanPrint)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية الطباعة");
            return;
        }

        try
        {
            PrintService.PrintReportTable(
                PermissionScreen,
                title,
                columns,
                rows,
                summaryLines,
                kind,
                DateFrom,
                DateTo,
                partyName);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    protected void ExportPdfDocument(
        string title,
        string partyName,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines,
        string suggestedFileName)
    {
        ExportStaffReportPdf(null, title, partyName, columns, rows, summaryLines, suggestedFileName);
    }

    /// <summary>Staff operational report PDF via document-kind key (full internal data allowed).</summary>
    protected void ExportStaffReportPdf(
        CustomsPrintDocumentKind? kind,
        string title,
        string partyName,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines,
        string suggestedFileName)
    {
        if (!CanExport)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية التصدير");
            return;
        }

        if (rows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للتصدير");
            return;
        }

        try
        {
            var model = new StatementPrintModel
            {
                Title = title,
                PartyName = partyName,
                FromDate = DateFrom,
                ToDate = DateTo,
                Columns = columns,
                Rows = rows,
                SummaryLines = summaryLines,
                IsCustomsReport = true
            };

            // H-04: always go through PrintService so EnsureExport(screen) cannot be bypassed.
            var temp = PrintService.ExportReportPdf(PermissionScreen, model, kind);

            var dlg = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = suggestedFileName,
                DefaultExt = ".pdf"
            };
            if (dlg.ShowDialog() == true)
            {
                File.Copy(temp, dlg.FileName, overwrite: true);
                BeautifulMessageDialog.ShowSuccess("تم حفظ ملف PDF");
            }
            else
            {
                Process.Start(new ProcessStartInfo { FileName = temp, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    protected void RegisterThemeChartReload(Func<Task> reload)
        => ThemeChartRefresh.Register(reload);
}
