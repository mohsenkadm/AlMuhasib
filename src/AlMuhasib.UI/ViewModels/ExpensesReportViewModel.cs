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

public partial class ExpensesReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalExpenses = "0";
    [ObservableProperty] private string _todayExpenses = "0";
    [ObservableProperty] private string _monthExpenses = "0";
    [ObservableProperty] private string _topExpenseType = "—";

    [ObservableProperty] private int? _selectedExpenseTypeId;
    [ObservableProperty] private int? _selectedCashBoxId;
    public ObservableCollection<ExpenseType> ExpenseTypes { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty] private ISeries[] _typeSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<ExpenseReportRow> _allRows = [];
    public ObservableCollection<ExpenseReportRow> Rows { get; } = [];

    public ExpensesReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    { PageTitle = "تقرير المصاريف"; }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var t in await _unitOfWork.ExpenseTypes.GetAllAsync()) ExpenseTypes.Add(t);
        foreach (var cb in await _unitOfWork.CashBoxes.GetAllAsync()) CashBoxes.Add(cb);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetExpensesReportAsync(DateFrom, DateTo, _selectedExpenseTypeId, _selectedCashBoxId);

            TotalExpenses = FormatCurrency(result.TotalExpenses);
            TodayExpenses = FormatCurrency(result.TodayExpenses);
            MonthExpenses = FormatCurrency(result.MonthExpenses);
            TopExpenseType = result.TopExpenseType;

            if (result.ByTypeChart.Count > 0)
                TypeSeries = ChartThemeConfig.PieFromNameAmount(result.ByTypeChart);

            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "المصاريف", 3)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_المصاريف.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "نوع المصروف", "المبلغ", "الصندوق", "ملاحظات", "بواسطة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.ExpenseTypeName, r.Amount, r.CashBoxName, r.Notes, r.CreatedBy }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "المصاريف", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "نوع المصروف", "المبلغ", "الصندوق", "ملاحظات", "بواسطة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.ExpenseTypeName, r.Amount, r.CashBoxName, r.Notes, r.CreatedBy }).ToList();
        _exportService.PrintTable("تقرير المصاريف", cols, rows);
    }
}
