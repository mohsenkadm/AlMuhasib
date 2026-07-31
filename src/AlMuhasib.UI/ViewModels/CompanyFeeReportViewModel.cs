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

public partial class CompanyFeeReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalFees = "0";
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _averageFeePercent = "0";
    [ObservableProperty] private string _invoiceCount = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<CompanyFeeRow> _allRows = [];
    public ObservableCollection<CompanyFeeRow> Rows { get; } = [];

    public CompanyFeeReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "عمولة المنصة / رسوم الشركة";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var x in await _unitOfWork.Customers.GetAllAsync()) Customers.Add(x);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetCompanyFeeReportAsync(DateFrom, DateTo, SelectedCustomerId);

            TotalFees = FormatCurrency(result.TotalFees);
            TotalSales = FormatCurrency(result.TotalSales);
            AverageFeePercent = result.AverageFeePercent.ToString("N1");
            InvoiceCount = result.InvoiceCount.ToString("N0");
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "عمولة_المنصة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "الفاتورة", "العميل", "صافي الفاتورة", "النسبة %", "العمولة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.InvoiceNumber, r.CustomerName, r.NetAmount, r.FeePercent, r.FeeAmount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "عمولة المنصة / رسوم الشركة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "الفاتورة", "العميل", "صافي الفاتورة", "النسبة %", "العمولة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.InvoiceNumber, r.CustomerName, r.NetAmount, r.FeePercent, r.FeeAmount }).ToList();
        _exportService.PrintTable("عمولة المنصة / رسوم الشركة", cols, rows);
    }
}
