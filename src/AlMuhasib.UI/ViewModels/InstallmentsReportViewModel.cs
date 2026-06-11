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

public partial class InstallmentsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalAmount = "0";
    [ObservableProperty] private string _paidAmount = "0";
    [ObservableProperty] private string _unpaidAmount = "0";
    [ObservableProperty] private string _overdueAmount = "0";
    [ObservableProperty] private string _totalCount = "0";
    [ObservableProperty] private string _paidCount = "0";
    [ObservableProperty] private string _unpaidCount = "0";
    [ObservableProperty] private string _overdueCount = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    [ObservableProperty] private string _selectedStatus = "الكل";
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<string> StatusOptions { get; } = ["الكل", "مسدد", "قيد التسديد", "متأخر"];

    [ObservableProperty] private ISeries[] _statusSeries = [];
    [ObservableProperty] private ISeries[] _collectionSeries = [];
    [ObservableProperty] private Axis[] _collectionXAxes = [];
    [ObservableProperty] private Axis[] _collectionYAxes = [];

    private List<InstallmentSummaryRow> _allRows = [];
    public ObservableCollection<InstallmentSummaryRow> Rows { get; } = [];

    public InstallmentsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "ملخص الأقساط";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        var customers = await _unitOfWork.Customers.GetAllAsync();
        foreach (var c in customers) Customers.Add(c);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetInstallmentsSummaryAsync(DateFrom, DateTo, _selectedCustomerId, _selectedStatus);

            TotalAmount = FormatCurrency(result.TotalAmount);
            PaidAmount = FormatCurrency(result.PaidAmount);
            UnpaidAmount = FormatCurrency(result.UnpaidAmount);
            OverdueAmount = FormatCurrency(result.OverdueAmount);
            TotalCount = result.TotalCount.ToString("N0");
            PaidCount = result.PaidCount.ToString("N0");
            UnpaidCount = result.UnpaidCount.ToString("N0");
            OverdueCount = result.OverdueCount.ToString("N0");

            if (result.StatusChart.Count > 0)
            {
                StatusSeries = result.StatusChart
                    .Where(s => s.Amount > 0)
                    .Select(s => (ISeries)ChartThemeConfig.Pie(s.Amount, s.Name, StatusPieColorIndex(s.Name)))
                    .ToArray();
            }

            if (result.MonthlyCollectionChart.Count > 0)
            {
                CollectionSeries = [ChartThemeConfig.Column(result.MonthlyCollectionChart.Select(d => d.Amount).ToArray(), "التحصيل الشهري", 0)];
                CollectionXAxes = [ChartThemeConfig.CreateXAxis(result.MonthlyCollectionChart.Select(d => d.Date.ToString("yyyy/MM")).ToArray(), -45)];
                CollectionYAxes = [ChartThemeConfig.CreateYAxis()];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "ملخص_الأقساط.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "العميل", "رقم الخطة", "الإجمالي", "المسدد", "المتبقي", "عدد الأقساط", "الحالة" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.PlanNumber, r.TotalAmount, r.PaidAmount, r.RemainingAmount, r.InstallmentCount, r.Status }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "ملخص الأقساط", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العميل", "رقم الخطة", "الإجمالي", "المسدد", "المتبقي", "عدد الأقساط", "الحالة" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.PlanNumber, r.TotalAmount, r.PaidAmount, r.RemainingAmount, r.InstallmentCount, r.Status }).ToList();
        _exportService.PrintTable("ملخص الأقساط", cols, rows);
    }

    /// <summary>ألوان ثابتة لكل حالة (مسدد=أخضر، متأخر=أحمر) لتطابق المفتاح مع القطعة في RTL.</summary>
    private static int StatusPieColorIndex(string statusName) => statusName switch
    {
        "مسدد" => 2,
        "جزئي" => 4,
        "معلق" => 0,
        "متأخر" => 3,
        _ => 1
    };
}
