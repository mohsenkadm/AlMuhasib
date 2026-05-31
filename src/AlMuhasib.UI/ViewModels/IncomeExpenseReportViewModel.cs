using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class IncomeExpenseReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalIncome = "0";
    [ObservableProperty] private string _totalExpenses = "0";
    [ObservableProperty] private string _netResult = "0";
    [ObservableProperty] private string _expenseRate = "0%";

    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];

    private List<IncomeExpenseRow> _allRows = [];
    public ObservableCollection<IncomeExpenseRow> Rows { get; } = [];

    public IncomeExpenseReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "الواردات والمصروفات";
        RegisterThemeChartReload(LoadDataAsync);
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
            var result = await _reportService.GetIncomeExpenseReportAsync(DateFrom, DateTo);

            TotalIncome = FormatCurrency(result.TotalIncome);
            TotalExpenses = FormatCurrency(result.TotalExpenses);
            NetResult = FormatCurrency(result.NetResult);
            ExpenseRate = $"{result.ExpenseRate}%";

            if (result.MonthlyChart.Count > 0)
            {
                MonthlySeries = [
                    ChartThemeConfig.Column(result.MonthlyChart.Select(m => m.Income).ToArray(), "الواردات", 0),
                    ChartThemeConfig.Column(result.MonthlyChart.Select(m => m.Expense).ToArray(), "المصروفات", 3)
                ];
                MonthlyXAxes = [ChartThemeConfig.CreateXAxis(result.MonthlyChart.Select(m => m.Month).ToArray(), -45)];
                MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "الواردات_والمصروفات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "القسم", "النوع", "البيان", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.Section, r.Type, r.Description, r.Amount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "الواردات والمصروفات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "القسم", "النوع", "البيان", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.Section, r.Type, r.Description, r.Amount }).ToList();
        _exportService.PrintTable("الواردات والمصروفات", cols, rows);
    }
}
