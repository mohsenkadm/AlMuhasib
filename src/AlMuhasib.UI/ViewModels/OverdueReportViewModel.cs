using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class OverdueReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _overdueCustomerCount = "0";
    [ObservableProperty] private string _totalOverdueAmount = "0";
    [ObservableProperty] private string _topOverdueCustomer = "—";
    [ObservableProperty] private string _averageOverdueDays = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    [ObservableProperty] private int? _minDaysOverdue;
    public ObservableCollection<Customer> Customers { get; } = [];

    [ObservableProperty] private ISeries[] _topCustomersSeries = [];
    [ObservableProperty] private ISeries[] _bucketSeries = [];
    [ObservableProperty] private Axis[] _bucketXAxes = [];
    [ObservableProperty] private Axis[] _bucketYAxes = [];

    private List<OverdueRow> _allRows = [];
    public ObservableCollection<OverdueRow> Rows { get; } = [];

    public OverdueReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService,
        IInvoiceService invoiceService, IInstallmentService installmentService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير المتأخرات";
        InitReportActionServices(invoiceService, installmentService);
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var c in await _unitOfWork.Customers.GetAllAsync()) Customers.Add(c);
        await LoadBulkPayCashBoxesAsync();
        await LoadDataAsync();
    }

    protected override async Task OnAfterBulkInstallmentPayAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetOverdueReportAsync(DateTime.Today, _minDaysOverdue, _selectedCustomerId);

            OverdueCustomerCount = result.OverdueCustomerCount.ToString("N0");
            TotalOverdueAmount = FormatCurrency(result.TotalOverdueAmount);
            TopOverdueCustomer = result.TopOverdueCustomer;
            AverageOverdueDays = $"{result.AverageOverdueDays} يوم";

            if (result.TopCustomersChart.Count > 0)
                TopCustomersSeries = ChartThemeConfig.PieFromNameAmount(result.TopCustomersChart);

            if (result.OverdueBucketChart.Count > 0)
            {
                BucketSeries = [ChartThemeConfig.Column(result.OverdueBucketChart.Select(b => b.Amount).ToArray(), "المبلغ المتأخر", 3)];
                BucketXAxes = [ChartThemeConfig.CreateXAxis(result.OverdueBucketChart.Select(b => b.Name).ToArray())];
                BucketYAxes = [ChartThemeConfig.CreateYAxis()];
            }

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_المتأخرات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "العميل", "الهاتف", "رقم الخطة", "المبلغ المتأخر", "أيام التأخير" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.Phone, r.PlanNumber, r.OverdueAmount, r.OverdueDays }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "المتأخرات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العميل", "الهاتف", "رقم الخطة", "المبلغ المتأخر", "أيام التأخير" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.Phone, r.PlanNumber, r.OverdueAmount, r.OverdueDays }).ToList();
        _exportService.PrintTable("تقرير المتأخرات", cols, rows);
    }
}
