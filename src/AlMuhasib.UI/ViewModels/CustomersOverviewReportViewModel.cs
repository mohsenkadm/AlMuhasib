using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class CustomersOverviewReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _totalCollected = "0";
    [ObservableProperty] private string _totalOutstanding = "0";
    [ObservableProperty] private string _customerCount = "0";

    private List<CustomerOverviewRow> _allRows = [];
    public ObservableCollection<CustomerOverviewRow> Rows { get; } = [];

    public CustomersOverviewReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "ملخص العملاء";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetCustomersOverviewReportAsync(DateFrom, DateTo);

            TotalSales = FormatCurrency(result.TotalSales);
            TotalCollected = FormatCurrency(result.TotalCollected);
            TotalOutstanding = FormatCurrency(result.TotalOutstanding);
            CustomerCount = result.CustomerCount.ToString("N0");

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "ملخص_العملاء.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "العميل", "الهاتف", "عدد الفواتير", "المبيعات", "المحصّل", "الرصيد المستحق" };
        var rows = _allRows.Select(r => new object[]
        {
            r.CustomerName, r.Phone, r.InvoiceCount, r.SalesAmount, r.CollectedAmount, r.OutstandingBalance
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "ملخص العملاء", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العميل", "الهاتف", "فواتير", "المبيعات", "المحصّل", "المستحق" };
        var rows = _allRows.Select(r => new object[]
        {
            r.CustomerName, r.Phone, r.InvoiceCount, r.SalesAmount, r.CollectedAmount, r.OutstandingBalance
        }).ToList();
        _exportService.PrintTable("ملخص العملاء", cols, rows);
    }
}
