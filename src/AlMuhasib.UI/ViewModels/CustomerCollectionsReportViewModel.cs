using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class CustomerCollectionsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalCollected = "0";
    [ObservableProperty] private string _voucherCollections = "0";
    [ObservableProperty] private string _installmentCollections = "0";
    [ObservableProperty] private string _rowCount = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];
    [ObservableProperty] private int? _selectedCashBoxId;
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<CustomerCollectionRow> _allRows = [];
    public ObservableCollection<CustomerCollectionRow> Rows { get; } = [];

    public CustomerCollectionsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "كشف تحصيلات العملاء";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var x in await _unitOfWork.Customers.GetAllAsync()) Customers.Add(x);
        foreach (var x in await _unitOfWork.CashBoxes.GetAllAsync()) CashBoxes.Add(x);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetCustomerCollectionsReportAsync(DateFrom, DateTo, SelectedCustomerId, SelectedCashBoxId);

            TotalCollected = FormatCurrency(result.TotalCollected);
            VoucherCollections = FormatCurrency(result.VoucherCollections);
            InstallmentCollections = FormatCurrency(result.InstallmentCollections);
            RowCount = result.RowCount.ToString("N0");
            if (result.ByCustomerChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.ByCustomerChart);
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            _allRows = result.Rows;

            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تحصيلات_العملاء.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "النوع", "العميل", "المرجع", "المبلغ", "الحساب" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.SourceType, r.CustomerName, r.Reference, r.Amount, r.AccountName }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "كشف تحصيلات العملاء", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "النوع", "العميل", "المرجع", "المبلغ", "الحساب" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.SourceType, r.CustomerName, r.Reference, r.Amount, r.AccountName }).ToList();
        _exportService.PrintTable("كشف تحصيلات العملاء", cols, rows);
    }
}
