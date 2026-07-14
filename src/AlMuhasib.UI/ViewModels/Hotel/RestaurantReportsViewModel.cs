using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class RestaurantReportsViewModel : ViewModelBase
{
    private readonly IRestaurantReportService _reportService;
    private readonly IRestaurantInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<RestaurantChannelSales> ChannelSales { get; } = [];
    public ObservableCollection<RestaurantTopItem> TopItems { get; } = [];
    public ObservableCollection<RestaurantPaymentBreakdown> PaymentBreakdown { get; } = [];
    public ObservableCollection<Core.Entities.Hotel.Restaurant.RestaurantIngredient> LowStock { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private DateTime _dateFrom = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;
    [ObservableProperty] private RestaurantProfitSummary? _profitSummary;
    [ObservableProperty] private RestaurantFinancialOverview? _financialOverview;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private ISeries[] _channelSeries = [];

    public bool HasFinancialOverview => FinancialOverview is not null;
    public string OverviewRevenue => FinancialOverview?.RestaurantRevenue.ToString("N0") ?? "0";
    public string OverviewCogs => FinancialOverview?.RestaurantCogs.ToString("N0") ?? "0";
    public string OverviewGrossProfit => FinancialOverview?.RestaurantGrossProfit.ToString("N0") ?? "0";
    public string OverviewKitchenPurchases => FinancialOverview?.KitchenPurchases.ToString("N0") ?? "0";
    public string OverviewNetOperating => FinancialOverview?.NetOperating.ToString("N0") ?? "0";

    public RestaurantReportsViewModel(
        IRestaurantReportService reportService,
        IRestaurantInventoryService inventoryService,
        ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
        PageTitle = "تقارير المطعم";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.RestaurantReports);
        await LoadReportsAsync();
    }

    [RelayCommand]
    private async Task SetPeriodTodayAsync()
    {
        DateFrom = DateTime.Today;
        DateTo = DateTime.Today;
        await LoadReportsAsync();
    }

    [RelayCommand]
    private async Task SetPeriod7DaysAsync()
    {
        DateFrom = DateTime.Today.AddDays(-6);
        DateTo = DateTime.Today;
        await LoadReportsAsync();
    }

    [RelayCommand]
    private async Task SetPeriod30DaysAsync()
    {
        DateFrom = DateTime.Today.AddDays(-29);
        DateTo = DateTime.Today;
        await LoadReportsAsync();
    }

    [RelayCommand]
    private async Task LoadReportsAsync()
    {
        IsBusy = true;
        try
        {
            ProfitSummary = await _reportService.GetProfitSummaryAsync(DateFrom, DateTo);
            FinancialOverview = await _reportService.GetFinancialOverviewAsync(DateFrom, DateTo);

            ChannelSales.Clear();
            foreach (var c in await _reportService.GetSalesByChannelAsync(DateFrom, DateTo))
                ChannelSales.Add(c);

            TopItems.Clear();
            foreach (var t in await _reportService.GetTopSellingItemsAsync(DateFrom, DateTo))
                TopItems.Add(t);

            PaymentBreakdown.Clear();
            foreach (var p in await _reportService.GetPaymentBreakdownAsync(DateFrom, DateTo))
                PaymentBreakdown.Add(p);

            var daily = await _reportService.GetDailySalesAsync(DateFrom, DateTo);
            if (daily.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(daily.Select(d => d.Revenue).ToArray(), "إيراد المطعم", 5)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(daily.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                DailySeries = [];
                DailyXAxes = [];
                DailyYAxes = [];
            }

            ChannelSeries = ChannelSales.Count > 0
                ? ChartThemeConfig.PieFromNameAmount(ChannelSales
                    .Select(c => new NameAmountPoint { Name = c.Label, Amount = c.Revenue })
                    .ToList())
                : [];

            LowStock.Clear();
            foreach (var i in await _inventoryService.GetLowStockAlertsAsync())
                LowStock.Add(i);

            LowStockCount = LowStock.Count;
            UpdateStats();
            NotifyOverview();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NotifyOverview()
    {
        OnPropertyChanged(nameof(HasFinancialOverview));
        OnPropertyChanged(nameof(OverviewRevenue));
        OnPropertyChanged(nameof(OverviewCogs));
        OnPropertyChanged(nameof(OverviewGrossProfit));
        OnPropertyChanged(nameof(OverviewKitchenPurchases));
        OnPropertyChanged(nameof(OverviewNetOperating));
    }

    private void UpdateStats()
    {
        Stats.Clear();
        if (ProfitSummary is not null)
        {
            Stats.Add(new HotelListStatItem { Label = "الإيراد", Value = ProfitSummary.Revenue.ToString("N0"), AccentColor = "#1565C0" });
            Stats.Add(new HotelListStatItem { Label = "التكلفة", Value = ProfitSummary.Cogs.ToString("N0"), AccentColor = "#F57C00" });
            Stats.Add(new HotelListStatItem { Label = "الربح", Value = ProfitSummary.GrossProfit.ToString("N0"), AccentColor = "#2E7D32" });
            Stats.Add(new HotelListStatItem { Label = "الهامش %", Value = ProfitSummary.MarginPercent.ToString("N1"), AccentColor = "#00897B" });
            Stats.Add(new HotelListStatItem { Label = "متوسط الطلب", Value = ProfitSummary.AverageOrderValue.ToString("N0"), AccentColor = "#455A64" });
            Stats.Add(new HotelListStatItem { Label = "خدمة الغرف", Value = ProfitSummary.RoomServiceRevenue.ToString("N0"), AccentColor = "#6A1B9A" });
        }

        var totalOrders = ChannelSales.Sum(c => c.OrderCount);
        Stats.Add(new HotelListStatItem { Label = "الطلبات", Value = totalOrders.ToString("N0"), AccentColor = "#00897B" });
        Stats.Add(new HotelListStatItem { Label = "تنبيهات المخزون", Value = LowStockCount.ToString("N0"), AccentColor = "#C62828" });
    }
}
