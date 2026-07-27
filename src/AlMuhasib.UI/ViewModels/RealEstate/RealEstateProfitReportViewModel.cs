using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstateProfitReportViewModel : ViewModelBase
{
    private readonly IRealEstateContractReportService _reportService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private RealEstateProfitReportData? _last;

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private bool _isDetailsVisible = true;

    [ObservableProperty] private string _saleRevenueText = "0";
    [ObservableProperty] private string _purchaseCostText = "0";
    [ObservableProperty] private string _grossProfitText = "0";
    [ObservableProperty] private string _totalExpensesText = "0";
    [ObservableProperty] private string _netProfitText = "0";
    [ObservableProperty] private string _marginText = "0%";
    [ObservableProperty] private string _netCashText = "0";
    [ObservableProperty] private string _receivablesText = "0";
    [ObservableProperty] private string _payablesText = "0";
    [ObservableProperty] private string _summaryLine = string.Empty;

    [ObservableProperty] private ISeries[] _monthlyNetSeries = [];
    [ObservableProperty] private ISeries[] _expensePieSeries = [];
    [ObservableProperty] private ISeries[] _plSeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];
    [ObservableProperty] private Axis[] _plXAxes = [];
    [ObservableProperty] private Axis[] _plYAxes = [];

    public ObservableCollection<RealEstateProfitContractRow> SaleRows { get; } = [];
    public ObservableCollection<RealEstateProfitContractRow> PurchaseRows { get; } = [];
    public ObservableCollection<RealEstateExpenseListItem> ExpenseRows { get; } = [];
    public ObservableCollection<RealEstateMonthlyProfitPoint> MonthlyRows { get; } = [];

    public RealEstateProfitReportViewModel(
        IRealEstateContractReportService reportService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _reportService = reportService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "تقرير الأرباح";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.ProfitReport);
        await LoadAsync();
    }

    [RelayCommand]
    private void ToggleDetails() => IsDetailsVisible = !IsDetailsVisible;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _reportService.GetProfitReportAsync(DateFrom, DateTo);
            _last = data;

            SaleRevenueText = Format(data.SaleRevenue);
            PurchaseCostText = Format(data.PurchaseCost);
            GrossProfitText = Format(data.GrossProfit);
            TotalExpensesText = Format(data.TotalExpenses);
            NetProfitText = Format(data.NetProfit);
            MarginText = $"{data.ProfitMarginPercent:N1}%";
            NetCashText = Format(data.NetCash);
            ReceivablesText = Format(data.SaleReceivables);
            PayablesText = Format(data.PurchasePayables);
            SummaryLine =
                $"بيع {data.SaleContractsCount} عقد · شراء {data.PurchaseContractsCount} عقد · {data.ExpenseCount} مصروف · " +
                $"من {data.DateFrom:yyyy/MM/dd} إلى {data.DateTo:yyyy/MM/dd}";

            SaleRows.Clear();
            foreach (var row in data.SaleRows) SaleRows.Add(row);
            PurchaseRows.Clear();
            foreach (var row in data.PurchaseRows) PurchaseRows.Add(row);
            ExpenseRows.Clear();
            foreach (var row in data.ExpenseRows) ExpenseRows.Add(row);
            MonthlyRows.Clear();
            foreach (var row in data.MonthlySeries) MonthlyRows.Add(row);

            var months = data.MonthlySeries;
            MonthlyNetSeries =
            [
                ChartThemeConfig.Column(months.Select(m => m.SaleRevenue).ToArray(), "إيراد البيع", 0),
                ChartThemeConfig.Column(months.Select(m => m.PurchaseCost).ToArray(), "تكلفة الشراء", 1),
                ChartThemeConfig.Column(months.Select(m => m.Expenses).ToArray(), "المصاريف", 2),
                ChartThemeConfig.Column(months.Select(m => m.NetProfit).ToArray(), "صافي الربح", 3)
            ];
            MonthlyXAxes = [ChartThemeConfig.CreateXAxis(months.Select(m => m.Period).ToArray(), -35)];
            MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];

            ExpensePieSeries = data.ExpensesByType
                .Select((p, i) => (ISeries)ChartThemeConfig.Pie(p.Amount, p.Name, i))
                .ToArray();

            PlSeries =
            [
                ChartThemeConfig.Column(
                [
                    data.SaleRevenue,
                    data.PurchaseCost,
                    data.GrossProfit,
                    data.TotalExpenses,
                    data.NetProfit
                ], "ملخص الفترة", 0)
            ];
            PlXAxes = [ChartThemeConfig.CreateXAxis(["إيراد", "تكلفة", "مجمل", "مصروف", "صافي"], 0)];
            PlYAxes = [ChartThemeConfig.CreateYAxis()];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport || _last is null) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"RealEstateProfit_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        var headers = new[] { "البند", "المبلغ" };
        var summary = new List<object?[]>
        {
            new object?[] { "إيراد عقود البيع", _last.SaleRevenue },
            new object?[] { "تكلفة عقود الشراء", _last.PurchaseCost },
            new object?[] { "مجمل الربح", _last.GrossProfit },
            new object?[] { "المصاريف التشغيلية", _last.TotalExpenses },
            new object?[] { "صافي الربح", _last.NetProfit },
            new object?[] { "هامش مجمل الربح %", _last.ProfitMarginPercent },
            new object?[] { "صافي التدفق النقدي", _last.NetCash },
            new object?[] { "ذمم مدينة (بيع)", _last.SaleReceivables },
            new object?[] { "ذمم دائنة (شراء)", _last.PurchasePayables }
        };
        _exportService.ExportToExcel(dialog.FileName, "الأرباح", headers, summary);
        _toast.ShowSuccess("تم تصدير تقرير الأرباح");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void PrintReport()
    {
        if (!CanPrint || _last is null) return;
        var headers = new[] { "البند", "المبلغ" };
        var rows = new List<object?[]>
        {
            new object?[] { "إيراد عقود البيع", _last.SaleRevenue },
            new object?[] { "تكلفة عقود الشراء", _last.PurchaseCost },
            new object?[] { "مجمل الربح", _last.GrossProfit },
            new object?[] { "المصاريف التشغيلية", _last.TotalExpenses },
            new object?[] { "صافي الربح", _last.NetProfit },
            new object?[] { "صافي التدفق النقدي", _last.NetCash }
        };
        _exportService.PrintTable(
            "تقرير أرباح عقود العقارات",
            headers,
            rows,
            [
                SummaryLine,
                "المعادلة: صافي الربح = (إيراد البيع − تكلفة الشراء) − المصاريف التشغيلية"
            ]);
    }

    private static string Format(decimal value) =>
        value.ToString("N0", CultureInfo.GetCultureInfo("ar-IQ"));
}
