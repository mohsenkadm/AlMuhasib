using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
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
    [ObservableProperty] private string _welcomeText = string.Empty;
    [ObservableProperty] private string _subtitleText = string.Empty;

    [ObservableProperty] private decimal _todaySalesIqd;
    [ObservableProperty] private decimal _todaySalesUsd;
    [ObservableProperty] private decimal _todayPurchasesIqd;
    [ObservableProperty] private decimal _todayPurchasesUsd;
    [ObservableProperty] private decimal _cashBalanceIqd;
    [ObservableProperty] private decimal _cashBalanceUsd;
    [ObservableProperty] private decimal _totalStockGrams;
    [ObservableProperty] private decimal _totalStockValueIqd;
    [ObservableProperty] private int _openCreditCount;
    [ObservableProperty] private decimal _openCreditIqd;
    [ObservableProperty] private int _overdueCreditCount;
    [ObservableProperty] private int _lowStockKaratCount;
    [ObservableProperty] private bool _pricesUpdatedToday;
    [ObservableProperty] private decimal? _latestUsdToIqd;
    [ObservableProperty] private string _todaySalesDisplay = "—";
    [ObservableProperty] private string _cashBalanceDisplay = "—";
    [ObservableProperty] private string _stockDisplay = "—";
    [ObservableProperty] private string _creditDisplay = "—";
    [ObservableProperty] private string _fxRateDisplay = "—";

    [ObservableProperty] private ISeries[] _stockSeries = [];
    [ObservableProperty] private ISeries[] _salesSeries = [];
    [ObservableProperty] private Axis[] _salesXAxes = [];
    [ObservableProperty] private Axis[] _salesYAxes = [];

    public ObservableCollection<GoldAlertItem> Alerts { get; } = [];
    public ObservableCollection<GoldInvoiceListItem> RecentInvoices { get; } = [];
    public ObservableCollection<GoldMithqalPriceRow> LatestPrices { get; } = [];
    public ObservableCollection<GoldStockRow> StockByKarat { get; } = [];

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
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Dashboard);
        var name = string.IsNullOrWhiteSpace(_currentUserService.Username)
            ? "مرحباً"
            : _currentUserService.Username;
        WelcomeText = $"أهلاً، {name}";
        SubtitleText = $"نظرة على محل الذهب — {DateTime.Now:dddd، d MMMM yyyy}";
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoaded = false;
        IsBusy = true;
        try
        {
            var data = await _dashboardService.GetDashboardAsync();
            ApplyDashboard(data);

            Alerts.Clear();
            var alerts = data.Alerts.Count > 0
                ? data.Alerts
                : (await _alertService.GetAlertsAsync()).ToList();
            foreach (var alert in alerts)
                Alerts.Add(alert);
        }
        catch (Exception ex)
        {
            Controls.BeautifulMessageDialog.ShowError($"تعذر تحميل لوحة التحكم:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    private void ApplyDashboard(GoldDashboardData data)
    {
        TodaySalesIqd = data.TodaySalesIqd;
        TodaySalesUsd = data.TodaySalesUsd;
        TodayPurchasesIqd = data.TodayPurchasesIqd;
        TodayPurchasesUsd = data.TodayPurchasesUsd;
        CashBalanceIqd = data.CashBalanceIqd;
        CashBalanceUsd = data.CashBalanceUsd;
        TotalStockGrams = data.TotalStockGrams;
        TotalStockValueIqd = data.TotalStockValueIqd;
        OpenCreditCount = data.OpenCreditCount;
        OpenCreditIqd = data.OpenCreditIqd;
        OverdueCreditCount = data.OverdueCreditCount;
        LowStockKaratCount = data.LowStockKaratCount;
        PricesUpdatedToday = data.PricesUpdatedToday;
        LatestUsdToIqd = data.LatestUsdToIqd;

        TodaySalesDisplay = $"{data.TodaySalesIqd:N0} د.ع\n{data.TodaySalesUsd:N2} $";
        CashBalanceDisplay = $"{data.CashBalanceIqd:N0} د.ع\n{data.CashBalanceUsd:N2} $";
        StockDisplay = $"{data.TotalStockGrams:N2} غ\n{data.TotalStockValueIqd:N0} د.ع";
        CreditDisplay = $"{data.OpenCreditCount} فاتورة\n{data.OpenCreditIqd:N0} د.ع";
        FxRateDisplay = data.LatestUsdToIqd.HasValue
            ? $"{data.LatestUsdToIqd.Value:N0} د.ع"
            : "غير محدد";

        StockByKarat.Clear();
        foreach (var row in data.StockByKarat)
            StockByKarat.Add(row);

        StockSeries = data.StockByKarat
            .Where(s => s.GramsOnHand > 0)
            .Select((s, i) => (ISeries)ChartThemeConfig.Pie(
                s.GramsOnHand,
                string.IsNullOrWhiteSpace(s.KaratName) ? $"عيار {s.KaratValue}" : s.KaratName,
                i))
            .ToArray();

        RecentInvoices.Clear();
        foreach (var invoice in data.RecentInvoices)
            RecentInvoices.Add(invoice);

        LatestPrices.Clear();
        foreach (var price in data.LatestPrices)
            LatestPrices.Add(price);

        BuildSalesPlaceholderChart(data.RecentInvoices);
    }

    private void BuildSalesPlaceholderChart(IEnumerable<GoldInvoiceListItem> invoices)
    {
        var sales = invoices
            .Where(i => i.InvoiceType == GoldInvoiceType.Sale)
            .GroupBy(i => i.InvoiceDate.Date)
            .OrderBy(g => g.Key)
            .TakeLast(7)
            .ToList();

        if (sales.Count == 0)
        {
            SalesSeries = [];
            SalesXAxes = [];
            SalesYAxes = [];
            return;
        }

        SalesSeries =
        [
            ChartThemeConfig.Column(sales.Select(g => g.Sum(x => x.TotalAmountIqd)).ToArray(), "مبيعات", 0)
        ];
        SalesXAxes = [ChartThemeConfig.CreateXAxis(sales.Select(g => g.Key.ToString("MM/dd")).ToArray(), -35)];
        SalesYAxes = [ChartThemeConfig.CreateYAxis()];
    }

    [RelayCommand]
    private async Task OpenSaleAsync() =>
        await _mainWindow.OpenTabAsync(typeof(GoldSaleInvoiceViewModel), "فاتورة بيع", PackIconKind.CashRegister);

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
}
