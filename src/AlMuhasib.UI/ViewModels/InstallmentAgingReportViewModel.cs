using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentAgingReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalOutstanding = "0";
    [ObservableProperty] private string _installmentCount = "0";
    [ObservableProperty] private string _customerCount = "0";
    [ObservableProperty] private DateTime _asOfDate = DateTime.Today;
    [ObservableProperty] private int? _selectedCustomerId;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<InstallmentAgingBucketSummary> BucketSummaries { get; } = [];

    [ObservableProperty] private ISeries[] _bucketSeries = [];
    [ObservableProperty] private Axis[] _bucketXAxes = [];
    [ObservableProperty] private Axis[] _bucketYAxes = [];

    private List<InstallmentAgingRow> _allRows = [];
    public ObservableCollection<InstallmentAgingRow> Rows { get; } = [];

    public InstallmentAgingReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService,
        IInvoiceService invoiceService, IInstallmentService installmentService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "أعمار ذمم الأقساط";
        InitReportActionServices(invoiceService, installmentService);
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var c in await _unitOfWork.Customers.GetAllAsync())
            Customers.Add(c);
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
            var result = await _reportService.GetInstallmentAgingReportAsync(AsOfDate, SelectedCustomerId);

            TotalOutstanding = FormatCurrency(result.TotalOutstanding);
            InstallmentCount = result.InstallmentCount.ToString("N0");
            CustomerCount = result.CustomerCount.ToString("N0");

            BucketSummaries.Clear();
            foreach (var b in result.Buckets)
                BucketSummaries.Add(b);

            if (result.Buckets.Count > 0)
            {
                BucketSeries = [ChartThemeConfig.Column(result.Buckets.Select(b => b.Amount).ToArray(), "المتبقي", 3)];
                BucketXAxes = [ChartThemeConfig.CreateXAxis(result.Buckets.Select(b => b.BucketName).ToArray(), -30)];
                BucketYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                BucketSeries = [];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "أعمار_الأقساط.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "العميل", "الهاتف", "الخطة", "الاستحقاق", "المبلغ", "المتبقي", "أيام التأخير", "الفئة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.CustomerName, r.Phone, r.PlanNumber,
            r.DueDate.ToString("yyyy/MM/dd"), r.Amount, r.RemainingAmount, r.DaysOverdue, r.AgingBucket
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "أعمار الأقساط", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العميل", "الهاتف", "الخطة", "الاستحقاق", "المبلغ", "المتبقي", "أيام التأخير", "الفئة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.CustomerName, r.Phone, r.PlanNumber,
            r.DueDate.ToString("yyyy/MM/dd"), r.Amount.ToString("N0"), r.RemainingAmount.ToString("N0"),
            r.DaysOverdue, r.AgingBucket
        }).ToList();
        _exportService.PrintTable("أعمار ذمم الأقساط", cols, rows);
    }
}
