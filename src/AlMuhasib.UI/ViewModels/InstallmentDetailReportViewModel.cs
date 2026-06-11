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

public partial class InstallmentDetailReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _customerName = "—";
    [ObservableProperty] private string _planCount = "0";
    [ObservableProperty] private string _totalAmount = "0";
    [ObservableProperty] private string _collectionRate = "0%";
    [ObservableProperty] private string _averageInstallment = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];

    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];

    private List<InstallmentDetailRow> _allRows = [];
    public ObservableCollection<InstallmentDetailRow> Rows { get; } = [];

    public InstallmentDetailReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تفاصيل الأقساط";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        var customers = await _unitOfWork.Customers.GetAllAsync();
        foreach (var c in customers) Customers.Add(c);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (_selectedCustomerId is null) { BeautifulMessageDialog.ShowWarning("يرجى اختيار عميل"); return; }
        try
        {
            IsBusy = true;
            var result = await _reportService.GetInstallmentDetailAsync(_selectedCustomerId.Value);

            CustomerName = result.CustomerName;
            PlanCount = result.PlanCount.ToString("N0");
            TotalAmount = FormatCurrency(result.TotalAmount);
            CollectionRate = $"{result.CollectionRate}%";
            AverageInstallment = FormatCurrency(result.AverageInstallment);

            if (result.MonthlyDueChart.Count > 0)
            {
                MonthlySeries = [ChartThemeConfig.Column(result.MonthlyDueChart.Select(d => d.Amount).ToArray(), "المستحق شهرياً", 0)];
                MonthlyXAxes = [ChartThemeConfig.CreateXAxis(result.MonthlyDueChart.Select(d => d.Date.ToString("yyyy/MM")).ToArray(), -45)];
                MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تفاصيل_الأقساط.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "تاريخ الاستحقاق", "المبلغ", "المسدد", "المتبقي", "تاريخ الدفع", "الخطة", "الحالة" };
        var rows = _allRows.Select(r => new object[] { r.DueDate.ToString("yyyy/MM/dd"), r.Amount, r.PaidAmount, r.RemainingAmount, r.PaymentDate?.ToString("yyyy/MM/dd") ?? "—", r.PlanNumber, r.Status }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "تفاصيل الأقساط", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "تاريخ الاستحقاق", "المبلغ", "المسدد", "المتبقي", "تاريخ الدفع", "الخطة", "الحالة" };
        var rows = _allRows.Select(r => new object[] { r.DueDate.ToString("yyyy/MM/dd"), r.Amount, r.PaidAmount, r.RemainingAmount, r.PaymentDate?.ToString("yyyy/MM/dd") ?? "—", r.PlanNumber, r.Status }).ToList();
        _exportService.PrintTable($"تفاصيل أقساط - {CustomerName}", cols, rows);
    }
}
