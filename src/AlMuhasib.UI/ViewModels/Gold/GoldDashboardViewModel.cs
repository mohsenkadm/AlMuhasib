using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldDashboardViewModel : ViewModelBase
{
    private readonly IGoldDashboardService _dashboardService;
    private readonly IGoldSmartAlertService _alertService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _welcomeGreeting = string.Empty;
    [ObservableProperty] private string _userDisplayName = string.Empty;
    [ObservableProperty] private string _displayDate = string.Empty;
    [ObservableProperty] private string _subtitleText = string.Empty;

    [ObservableProperty] private decimal _todaySalesIqd;
    [ObservableProperty] private decimal _todaySalesUsd;
    [ObservableProperty] private decimal _todayPurchasesIqd;
    [ObservableProperty] private decimal _todayPurchasesUsd;
    [ObservableProperty] private decimal _todayExpensesIqd;
    [ObservableProperty] private decimal _todayExpensesUsd;
    [ObservableProperty] private decimal _cashBalanceIqd;
    [ObservableProperty] private decimal _cashBalanceUsd;
    [ObservableProperty] private decimal _totalStockGrams;
    [ObservableProperty] private decimal _totalStockValueIqd;
    [ObservableProperty] private int _openCreditCount;
    [ObservableProperty] private decimal _openCreditIqd;
    [ObservableProperty] private int _overdueCreditCount;
    [ObservableProperty] private int _lowStockKaratCount;
    [ObservableProperty] private int _lowWarehouseStockCount;
    [ObservableProperty] private bool _pricesUpdatedToday;
    [ObservableProperty] private bool _hasExpenseToday;
    [ObservableProperty] private decimal? _latestUsdToIqd;

    [ObservableProperty] private string _todaySalesDisplay = "—";
    [ObservableProperty] private string _todayPurchasesDisplay = "—";
    [ObservableProperty] private string _todayExpensesDisplay = "—";
    [ObservableProperty] private string _cashBalanceDisplay = "—";
    [ObservableProperty] private string _stockDisplay = "—";
    [ObservableProperty] private string _stockGramsDisplay = "—";
    [ObservableProperty] private string _inventoryValueDisplay = "—";
    [ObservableProperty] private string _creditDisplay = "—";
    [ObservableProperty] private string _overdueDisplay = "—";
    [ObservableProperty] private string _fxRateDisplay = "—";
    [ObservableProperty] private int _dailyTaskCount;
    [ObservableProperty] private int _smartAlertCount;

    [ObservableProperty] private int _todayReturnCount;
    [ObservableProperty] private decimal _todayReturnIqd;
    [ObservableProperty] private int _todayExchangeCount;
    [ObservableProperty] private decimal _todayExchangeCashDiffIqd;
    [ObservableProperty] private int _supplierCreditCount;
    [ObservableProperty] private decimal _supplierCreditIqd;
    [ObservableProperty] private string _todayReturnsDisplay = "—";
    [ObservableProperty] private string _todayExchangesDisplay = "—";
    [ObservableProperty] private string _supplierCreditDisplay = "—";

    [ObservableProperty] private bool _showQuickSale = true;
    [ObservableProperty] private bool _showQuickPurchase = true;
    [ObservableProperty] private bool _showQuickExchange = true;
    [ObservableProperty] private bool _showQuickMithqal = true;

    [ObservableProperty] private ISeries[] _stockSeries = [];
    [ObservableProperty] private ISeries[] _salesSeries = [];
    [ObservableProperty] private Axis[] _salesXAxes = [];
    [ObservableProperty] private Axis[] _salesYAxes = [];

    public ObservableCollection<GoldAlertItem> Alerts { get; } = [];
    public ObservableCollection<DailyTaskItem> DailyTasks { get; } = [];
    public ObservableCollection<GoldInvoiceListItem> RecentInvoices { get; } = [];
    public ObservableCollection<GoldInvoiceListItem> RecentReturns { get; } = [];
    public ObservableCollection<GoldInvoiceListItem> RecentExchanges { get; } = [];
    public ObservableCollection<GoldMithqalPriceRow> LatestPrices { get; } = [];
    public ObservableCollection<GoldStockRow> StockByKarat { get; } = [];
    public ObservableCollection<GoldCashBoxSummary> CashBoxes { get; } = [];

    public GoldDashboardViewModel(
        IGoldDashboardService dashboardService,
        IGoldSmartAlertService alertService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _dashboardService = dashboardService;
        _alertService = alertService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "لوحة التحكم";
        GoldFxRateRefreshHelper.Register(this, ApplyBroadcastFxRateAsync);
    }

    private Task ApplyBroadcastFxRateAsync(decimal rate)
    {
        LatestUsdToIqd = rate;
        FxRateDisplay = $"1 USD = {rate:N0} IQD";
        return Task.CompletedTask;
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Dashboard);
        ApplyQuickActionVisibility();

        WelcomeGreeting = GetTimeGreeting();
        UserDisplayName = string.IsNullOrWhiteSpace(_currentUserService.Username)
            ? "مرحباً"
            : _currentUserService.Username;
        DisplayDate = DateTime.Now.ToString("dddd، d MMMM yyyy");
        SubtitleText = "نظرة شاملة على أداء محل الذهب — مؤشرات محدّثة لحظياً";
        await LoadDataAsync();
    }

    private void ApplyQuickActionVisibility()
    {
        ShowQuickSale = _currentUserService.CanView(GoldShopPermissionRegistry.SaleInvoice);
        ShowQuickPurchase = _currentUserService.CanView(GoldShopPermissionRegistry.PurchaseInvoice);
        ShowQuickExchange = _currentUserService.CanView(GoldShopPermissionRegistry.ExchangeInvoice);
        ShowQuickMithqal = _currentUserService.CanView(GoldShopPermissionRegistry.MithqalPrices);
    }

    private static string GetTimeGreeting()
    {
        var hour = DateTime.Now.Hour;
        if (hour < 12) return "صباح الخير";
        if (hour < 17) return "مساء الخير";
        return "مساء الخير";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _dashboardService.GetDashboardAsync();

            // Apply KPIs first so the page becomes interactive quickly.
            ApplyDashboard(data);

            Alerts.Clear();
            foreach (var alert in data.Alerts)
                Alerts.Add(alert);
            SmartAlertCount = Alerts.Count;

            // Build daily tasks from already-loaded dashboard data (no second DB round-trip).
            DailyTasks.Clear();
            foreach (var task in BuildDailyTasksFromDashboard(data))
                DailyTasks.Add(task);
            DailyTaskCount = DailyTasks.Count;

            IsLoaded = true;
        }
        catch (Exception ex)
        {
            IsLoaded = true;
            Controls.BeautifulMessageDialog.ShowError($"تعذر تحميل لوحة التحكم:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static List<DailyTaskItem> BuildDailyTasksFromDashboard(GoldDashboardData data)
    {
        var tasks = new List<DailyTaskItem>();
        if (!data.PricesUpdatedToday)
        {
            tasks.Add(new DailyTaskItem
            {
                Title = "تحديث أسعار المثقال",
                Description = "أسعار اليوم غير مسجّلة — حدّث التسعير قبل البيع",
                Action = SmartAlertAction.OpenGoldMithqalPrices,
                Priority = 1
            });
        }

        if (data.OverdueCreditCount > 0)
        {
            tasks.Add(new DailyTaskItem
            {
                Title = "تحصيل الذمم المتأخرة",
                Description = $"{data.OverdueCreditCount} زبون لديهم ذمم متأخرة",
                Action = SmartAlertAction.OpenGoldCollection,
                Priority = 2
            });
        }

        if (data.LowStockKaratCount > 0)
        {
            tasks.Add(new DailyTaskItem
            {
                Title = "مراجعة المخزون المنخفض",
                Description = $"{data.LowStockKaratCount} عيار تحت حد التنبيه",
                Action = SmartAlertAction.OpenGoldStock,
                Priority = 3
            });
        }

        if (!data.HasExpenseToday)
        {
            tasks.Add(new DailyTaskItem
            {
                Title = "تسجيل مصروف اليوم",
                Description = "لا يوجد مصروف مسجّل اليوم — أضف مصروفاً إن وُجد",
                Action = SmartAlertAction.OpenGoldExpenses,
                Priority = 4
            });
        }

        return tasks.OrderBy(t => t.Priority).ToList();
    }

    private void ApplyDashboard(GoldDashboardData data)
    {
        TodaySalesIqd = data.TodaySalesIqd;
        TodaySalesUsd = data.TodaySalesUsd;
        TodayPurchasesIqd = data.TodayPurchasesIqd;
        TodayPurchasesUsd = data.TodayPurchasesUsd;
        TodayExpensesIqd = data.TodayExpensesIqd;
        TodayExpensesUsd = data.TodayExpensesUsd;
        HasExpenseToday = data.HasExpenseToday;
        CashBalanceIqd = data.CashBalanceIqd;
        CashBalanceUsd = data.CashBalanceUsd;
        TotalStockGrams = data.TotalStockGrams;
        TotalStockValueIqd = data.TotalStockValueIqd;
        OpenCreditCount = data.OpenCreditCount;
        OpenCreditIqd = data.OpenCreditIqd;
        OverdueCreditCount = data.OverdueCreditCount;
        LowStockKaratCount = data.LowStockKaratCount;
        LowWarehouseStockCount = data.LowWarehouseStockCount;
        PricesUpdatedToday = data.PricesUpdatedToday;
        LatestUsdToIqd = data.LatestUsdToIqd;

        TodaySalesDisplay = FormatDual(data.TodaySalesIqd, data.TodaySalesUsd);
        TodayPurchasesDisplay = FormatDual(data.TodayPurchasesIqd, data.TodayPurchasesUsd);
        TodayExpensesDisplay = FormatDual(data.TodayExpensesIqd, data.TodayExpensesUsd);
        CashBalanceDisplay = FormatDual(data.CashBalanceIqd, data.CashBalanceUsd);
        StockGramsDisplay = $"{data.TotalStockGrams:N2} غ";
        StockDisplay = $"{data.TotalStockGrams:N2} غ\n{data.TotalStockValueIqd:N0} د.ع";
        InventoryValueDisplay = $"{data.TotalStockValueIqd:N0} د.ع";
        CreditDisplay = $"{data.OpenCreditCount} زبون\n{data.OpenCreditIqd:N0} د.ع";
        OverdueDisplay = $"{data.OverdueCreditCount} متأخر";
        FxRateDisplay = data.LatestUsdToIqd.HasValue
            ? $"{data.LatestUsdToIqd.Value:N0} د.ع"
            : "غير محدد";

        StockByKarat.Clear();
        foreach (var row in data.StockByKarat)
            StockByKarat.Add(row);

        StockSeries = data.StockByKarat
            .GroupBy(s => new { s.KaratValue, s.KaratName })
            .Select(g => new
            {
                g.Key.KaratValue,
                g.Key.KaratName,
                Grams = g.Sum(x => x.GramsOnHand)
            })
            .Where(s => s.Grams > 0)
            .Select((s, i) => (ISeries)ChartThemeConfig.Pie(
                s.Grams,
                string.IsNullOrWhiteSpace(s.KaratName) ? $"عيار {s.KaratValue}" : s.KaratName,
                i))
            .ToArray();

        BuildSalesChart(data.SalesLast30Days);

        RecentInvoices.Clear();
        foreach (var invoice in data.RecentInvoices)
            RecentInvoices.Add(invoice);

        RecentReturns.Clear();
        foreach (var invoice in data.RecentReturns)
            RecentReturns.Add(invoice);

        RecentExchanges.Clear();
        foreach (var invoice in data.RecentExchanges)
            RecentExchanges.Add(invoice);

        TodayReturnCount = data.TodayReturnCount;
        TodayReturnIqd = data.TodayReturnIqd;
        TodayExchangeCount = data.TodayExchangeCount;
        TodayExchangeCashDiffIqd = data.TodayExchangeCashDiffIqd;
        SupplierCreditCount = data.SupplierCreditCount;
        SupplierCreditIqd = data.SupplierCreditIqd;
        TodayReturnsDisplay = $"{data.TodayReturnCount} مرتجع\n{data.TodayReturnIqd:N0} د.ع";
        TodayExchangesDisplay = $"{data.TodayExchangeCount} عملية\n{data.TodayExchangeCashDiffIqd:N0} د.ع";
        SupplierCreditDisplay = $"{data.SupplierCreditCount} مورد\n{data.SupplierCreditIqd:N0} د.ع";

        LatestPrices.Clear();
        foreach (var price in data.LatestPrices)
            LatestPrices.Add(price);

        CashBoxes.Clear();
        foreach (var box in data.CashBoxes)
            CashBoxes.Add(box);
    }

    private void BuildSalesChart(List<DailySalesPoint> points)
    {
        if (points.Count == 0)
        {
            SalesSeries = [];
            SalesXAxes = [ChartThemeConfig.CreateXAxis([])];
            SalesYAxes = [ChartThemeConfig.CreateYAxis()];
            return;
        }

        var amounts = points.Select(p => p.Amount).ToArray();
        var labels = points.Select(p => p.Date.ToString("MM/dd")).ToArray();

        SalesSeries = [ChartThemeConfig.Line(amounts, "المبيعات", 0)];
        SalesXAxes = [ChartThemeConfig.CreateXAxis(labels, points.Count > 10 ? -35 : 0)];
        SalesYAxes = [ChartThemeConfig.CreateYAxis()];
    }

    private static string FormatDual(decimal iqd, decimal usd) =>
        $"{iqd:N0} د.ع\n{usd:N2} $";

    [RelayCommand]
    private async Task ExecuteDailyTaskAsync(DailyTaskItem? task)
    {
        if (task is null || task.Action == SmartAlertAction.None) return;
        await _mainWindow.ExecuteDailyTaskAsync(task.Action);
    }

    [RelayCommand]
    private async Task ExecuteAlertAsync(GoldAlertItem? alert)
    {
        if (alert is null) return;
        var action = MapAlertAction(alert.Type);
        if (action == SmartAlertAction.None) return;
        await _mainWindow.ExecuteDailyTaskAsync(action);
    }

    private static SmartAlertAction MapAlertAction(GoldNotificationType type) => type switch
    {
        GoldNotificationType.PriceNotUpdated => SmartAlertAction.OpenGoldMithqalPrices,
        GoldNotificationType.OverdueCredit => SmartAlertAction.OpenGoldCollection,
        GoldNotificationType.LowStock => SmartAlertAction.OpenGoldStock,
        GoldNotificationType.LowWarehouseStock => SmartAlertAction.OpenGoldWarehouses,
        GoldNotificationType.NoExpenseToday => SmartAlertAction.OpenGoldExpenses,
        GoldNotificationType.NegativeCash => SmartAlertAction.OpenGoldExpenses,
        _ => SmartAlertAction.None
    };

    [RelayCommand]
    private async Task OpenSaleAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldSaleInvoiceViewModel), "فاتورة بيع", PackIconKind.CashRegister);

    [RelayCommand]
    private async Task OpenExchangeAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldExchangeInvoiceViewModel), "تبديل ذهب", PackIconKind.SwapHorizontal);

    [RelayCommand]
    private async Task OpenExpenseAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldExpensesViewModel), "مصروف جديد", PackIconKind.CashMinus);

    [RelayCommand]
    private async Task OpenWarehouseTransferAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldWarehouseTransferViewModel), "نقل مخزني", PackIconKind.TruckDelivery);

    [RelayCommand]
    private async Task OpenPurchaseAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldPurchaseInvoiceViewModel), "فاتورة شراء", PackIconKind.CartArrowDown);

    [RelayCommand]
    private async Task OpenMithqalPricesAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldMithqalPricesViewModel), "أسعار المثقال", PackIconKind.CurrencyUsd);

    [RelayCommand]
    private async Task OpenCollectionAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldCollectionViewModel), "التحصيل", PackIconKind.CashCheck);

    [RelayCommand]
    private async Task OpenStockAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldStockViewModel), "المخزون", PackIconKind.Warehouse);

    [RelayCommand]
    private async Task OpenFxRatesAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldFxRatesViewModel), "أسعار الصرف", PackIconKind.CashMultiple);

    [RelayCommand]
    private async Task OpenCashBoxesAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldCashBoxesViewModel), "القاصات", PackIconKind.SafeSquareOutline);

    [RelayCommand]
    private async Task OpenExchangeReportAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldExchangeReportViewModel), "تقرير التبديل", PackIconKind.SwapHorizontal);

    [RelayCommand]
    private async Task OpenSaleReturnsReportAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldSaleReturnsReportViewModel), "تقرير مرتجعات البيع", PackIconKind.BackupRestore);
}
