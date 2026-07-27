using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.CarTrade;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.CarTrade;

public partial class CarTradeReportsViewModel : ViewModelBase
{
    private readonly ICarTradeReportService _reportService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private CarTradeStatusFilter _statusFilter = CarTradeStatusFilter.All;
    [ObservableProperty] private int _buyCount;
    [ObservableProperty] private int _sellCount;
    [ObservableProperty] private decimal _totalBuyValue;
    [ObservableProperty] private decimal _totalSellValue;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private ISeries[] _monthlyBuySeries = [];
    [ObservableProperty] private ISeries[] _monthlySellSeries = [];
    [ObservableProperty] private ISeries[] _amountSeries = [];
    [ObservableProperty] private ISeries[] _typeSeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];

    public ObservableCollection<CarTradeListItem> Rows { get; } = [];

    public CarTradeReportsViewModel(
        ICarTradeReportService reportService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _reportService = reportService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "التقارير";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarTradePermissionRegistry.CarTradeReports);
        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task LoadReportAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _reportService.GetReportAsync(new CarTradeFilter
            {
                DateFrom = DateFrom,
                DateTo = DateTo,
                StatusFilter = StatusFilter
            });

            Rows.Clear();
            foreach (var row in data.Rows)
                Rows.Add(row);

            BuyCount = data.BuyCount;
            SellCount = data.SellCount;
            TotalBuyValue = data.TotalBuyValue;
            TotalSellValue = data.TotalSellValue;
            TotalPaid = data.TotalPaid;
            TotalRemaining = data.TotalRemaining;

            var monthlyBuy = data.MonthlyBuy;
            var monthlySell = data.MonthlySell;
            var labels = monthlyBuy.Select(m => m.Name)
                .Union(monthlySell.Select(m => m.Name))
                .Distinct()
                .OrderBy(n => n)
                .ToArray();

            MonthlyBuySeries =
            [
                ChartThemeConfig.Column(
                    labels.Select(l => (decimal)(monthlyBuy.FirstOrDefault(m => m.Name == l)?.Count ?? 0)).ToArray(),
                    "شراء", 2)
            ];
            MonthlySellSeries =
            [
                ChartThemeConfig.Column(
                    labels.Select(l => (decimal)(monthlySell.FirstOrDefault(m => m.Name == l)?.Count ?? 0)).ToArray(),
                    "بيع", 0)
            ];
            MonthlyXAxes = [ChartThemeConfig.CreateXAxis(labels, -45)];
            MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];

            AmountSeries = ChartThemeConfig.PieFromNameAmount(data.CollectedVsRemaining);
            TypeSeries = data.ByCarType.Select((p, i) =>
                (ISeries)ChartThemeConfig.Pie(p.Count, p.Name, i)).ToArray();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport || Rows.Count == 0)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"CarTradeReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        var headers = new[] { "رقم العملية", "التاريخ", "النوع", "السيارة", "البائع", "المشتري", "الإجمالي", "المدفوع", "المتبقي" };
        var data = Rows.Select(r => new object?[]
        {
            r.TransactionNumber, r.TransactionDate.ToString("yyyy/MM/dd"), r.TradeType, r.CarName,
            r.SellerName, r.BuyerName, r.TotalAmount, r.AmountPaid, r.RemainingAmount
        }).ToList();

        _exportService.ExportToExcel(dialog.FileName, "تقرير العمليات", headers, data);
        _toast.ShowSuccess("تم تصدير الملف بنجاح");
    }

    [RelayCommand]
    private void PrintReport()
    {
        if (!CanPrint || Rows.Count == 0)
            return;

        var cols = new[] { "رقم العملية", "التاريخ", "النوع", "السيارة", "البائع", "المشتري", "الإجمالي", "المدفوع", "المتبقي" };
        var tableRows = Rows.Select(r => new object[]
        {
            r.TransactionNumber, r.TransactionDate.ToString("yyyy/MM/dd"), r.TradeType, r.CarName,
            r.SellerName, r.BuyerName, r.TotalAmount, r.AmountPaid, r.RemainingAmount
        }).ToList();

        var summary = new List<string>
        {
            $"عمليات شراء: {BuyCount} — القيمة: {TotalBuyValue:N0}",
            $"عمليات بيع: {SellCount} — القيمة: {TotalSellValue:N0}",
            $"المحصّل: {TotalPaid:N0} — المتبقي: {TotalRemaining:N0}"
        };

        if (DateFrom.HasValue || DateTo.HasValue)
        {
            summary.Insert(0,
                $"الفترة: {DateFrom?.ToString("yyyy/MM/dd") ?? "—"} إلى {DateTo?.ToString("yyyy/MM/dd") ?? "—"}");
        }

        _exportService.PrintTable("تقرير عمليات بيع وشراء السيارات", cols, tableRows, summary);
    }
}
