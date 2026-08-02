using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

/// <summary>Shared pagination / export / print helpers for Gold advanced reports.</summary>
public abstract partial class GoldReportViewModelBase : ViewModelBase
{
    protected readonly IGoldReportService ReportService;
    protected readonly IExportService ExportService;
    protected readonly IToastNotificationService Toast;
    protected readonly ICurrentUserService CurrentUserService;

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalRecords;
    [ObservableProperty] private int _pageSize = 25;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    protected GoldReportViewModelBase(
        IGoldReportService reportService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        ReportService = reportService;
        ExportService = exportService;
        Toast = toast;
        CurrentUserService = currentUserService;
    }

    protected void UpdatePagination<T>(IList<T> allItems, ObservableCollection<T> displayItems)
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

    protected void ExportTable(string fileName, string sheetTitle, string[] columns, IList<object[]> rows)
    {
        if (!CanExport)
        {
            Toast.ShowWarning("ليس لديك صلاحية التصدير");
            return;
        }

        var dlg = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = fileName };
        if (dlg.ShowDialog() != true) return;
        ExportService.ExportToExcel(dlg.FileName, sheetTitle, columns, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    protected void PrintTable(string title, string[] columns, IList<object[]> rows)
    {
        if (!CanPrint)
        {
            Toast.ShowWarning("ليس لديك صلاحية الطباعة");
            return;
        }

        ExportService.PrintTable(title, columns, rows);
    }

    protected static string FormatCurrency(decimal value) => $"{value:N0} د.ع";
}
