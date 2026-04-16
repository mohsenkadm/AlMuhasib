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

public partial class PaidInstallmentsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalPaid = "0";
    [ObservableProperty] private string _paidCount = "0";
    [ObservableProperty] private string _maxPaid = "0";
    [ObservableProperty] private string _averagePaymentDays = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    [ObservableProperty] private int? _selectedCashBoxId;
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];
    [ObservableProperty] private ISeries[] _cashBoxSeries = [];

    private List<PaidInstallmentRow> _allRows = [];
    public ObservableCollection<PaidInstallmentRow> Rows { get; } = [];

    public PaidInstallmentsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    { PageTitle = "الأقساط المسددة"; }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var c in await _unitOfWork.Customers.GetAllAsync()) Customers.Add(c);
        foreach (var cb in await _unitOfWork.CashBoxes.GetAllAsync()) CashBoxes.Add(cb);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetPaidInstallmentsAsync(DateFrom, DateTo, _selectedCustomerId, _selectedCashBoxId);

            TotalPaid = FormatCurrency(result.TotalPaid);
            PaidCount = result.PaidCount.ToString("N0");
            MaxPaid = FormatCurrency(result.MaxPaid);
            AveragePaymentDays = $"{result.AveragePaymentDays:N0} يوم";

            if (result.MonthlyChart.Count > 0)
            {
                MonthlySeries = [ChartThemeConfig.Column(result.MonthlyChart.Select(d => d.Amount).ToArray(), "التحصيل", 2)];
                MonthlyXAxes = [ChartThemeConfig.CreateXAxis(result.MonthlyChart.Select(d => d.Date.ToString("yyyy/MM")).ToArray(), -45)];
                MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];
            }

            if (result.ByCashBoxChart.Count > 0)
                CashBoxSeries = ChartThemeConfig.PieFromNameAmount(result.ByCashBoxChart);

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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "الأقساط_المسددة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "العميل", "رقم الخطة", "المبلغ", "تاريخ الدفع", "الصندوق" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.PlanNumber, r.Amount, r.PaymentDate.ToString("yyyy/MM/dd"), r.CashBoxName }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "الأقساط المسددة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العميل", "رقم الخطة", "المبلغ", "تاريخ الدفع", "الصندوق" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.PlanNumber, r.Amount, r.PaymentDate.ToString("yyyy/MM/dd"), r.CashBoxName }).ToList();
        _exportService.PrintTable("الأقساط المسددة", cols, rows);
    }
}
