using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.CarTrade;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.CarTrade;

public partial class CarTradeDashboardViewModel : ViewModelBase
{
    private readonly ICarTradeService _tradeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private int _todayTransactions;
    [ObservableProperty] private int _monthTransactions;
    [ObservableProperty] private int _unpaidTransactions;
    [ObservableProperty] private int _buyCount;
    [ObservableProperty] private int _sellCount;
    [ObservableProperty] private decimal _totalBuyValue;
    [ObservableProperty] private decimal _totalSellValue;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private ISeries[] _monthlyBuySeries = [];
    [ObservableProperty] private ISeries[] _monthlySellSeries = [];
    [ObservableProperty] private ISeries[] _statusSeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];

    public ObservableCollection<CarTradeListItem> RecentTransactions { get; } = [];

    public CarTradeDashboardViewModel(
        ICarTradeService tradeService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _tradeService = tradeService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "لوحة التحكم";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarTradePermissionRegistry.Dashboard);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoaded = false;
        try
        {
            var stats = await _tradeService.GetDashboardStatsAsync();
            TodayTransactions = stats.TodayTransactions;
            MonthTransactions = stats.MonthTransactions;
            UnpaidTransactions = stats.UnpaidTransactions;
            BuyCount = stats.BuyCount;
            SellCount = stats.SellCount;
            TotalBuyValue = stats.TotalBuyValue;
            TotalSellValue = stats.TotalSellValue;
            TotalPaid = stats.TotalPaid;
            TotalRemaining = stats.TotalRemaining;

            RecentTransactions.Clear();
            foreach (var item in stats.RecentTransactions)
                RecentTransactions.Add(item);

            var monthlyBuy = stats.MonthlyBuy;
            var monthlySell = stats.MonthlySell;
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

            StatusSeries = stats.PaymentStatusChart
                .Select((p, i) => (ISeries)ChartThemeConfig.Pie(p.Amount, p.Name, i))
                .ToArray();
        }
        finally
        {
            IsLoaded = true;
        }
    }

    [RelayCommand]
    private async Task OpenTransactionsAsync() =>
        await _mainWindow.OpenTabAsync(typeof(CarTradeListViewModel), "العمليات", PackIconKind.FormatListBulleted);

    [RelayCommand]
    private async Task OpenNewTransactionAsync() =>
        await _mainWindow.OpenTabAsync(typeof(CarTradeFormViewModel), "عملية جديدة", PackIconKind.SwapHorizontal, activateIfExists: false);
}
