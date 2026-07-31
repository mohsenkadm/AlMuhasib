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

public partial class InstallmentScheduleReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalAmount = "0";
    [ObservableProperty] private string _totalPaid = "0";
    [ObservableProperty] private string _totalRemaining = "0";
    [ObservableProperty] private string _installmentCount = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];
    [ObservableProperty] private string? _selectedStatus;
    public ObservableCollection<string> StatusOptions { get; } = ["", "Pending", "PartiallyPaid", "Paid", "Overdue"];

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<InstallmentScheduleReportRow> _allRows = [];
    public ObservableCollection<InstallmentScheduleReportRow> Rows { get; } = [];

    public InstallmentScheduleReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "جدول الاستحقاق";
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
            var result = await _reportService.GetInstallmentScheduleReportAsync(DateFrom, DateTo, SelectedCustomerId, SelectedStatus);

            TotalAmount = FormatCurrency(result.TotalAmount);
            TotalPaid = FormatCurrency(result.TotalPaid);
            TotalRemaining = FormatCurrency(result.TotalRemaining);
            InstallmentCount = result.InstallmentCount.ToString("N0");
            if (result.StatusChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.StatusChart);
            if (result.DueChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DueChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DueChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "جدول_الاستحقاق.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "الاستحقاق", "العميل", "المبلغ", "المسدد", "المتبقي", "الحالة", "تاريخ الدفع" };
        var rows = _allRows.Select(r => new object[] { r.DueDate.ToString("yyyy/MM/dd"), r.CustomerName, r.Amount, r.PaidAmount, r.RemainingAmount, r.Status, r.PaymentDate?.ToString("yyyy/MM/dd") ?? "—" }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "جدول الاستحقاق", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "الاستحقاق", "العميل", "المبلغ", "المسدد", "المتبقي", "الحالة", "تاريخ الدفع" };
        var rows = _allRows.Select(r => new object[] { r.DueDate.ToString("yyyy/MM/dd"), r.CustomerName, r.Amount, r.PaidAmount, r.RemainingAmount, r.Status, r.PaymentDate?.ToString("yyyy/MM/dd") ?? "—" }).ToList();
        _exportService.PrintTable("جدول الاستحقاق", cols, rows);
    }
}
