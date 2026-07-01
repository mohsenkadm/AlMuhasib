using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Core.Models.Ux;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MaterialDesignThemes.Wpf;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ISmartAlertService _smartAlertService;
    private readonly MainWindowViewModel _mainWindow;
    private bool _initialized;
    private List<DailySalesPoint>? _cachedSalesPoints;
    private List<ExpenseCategoryShare>? _cachedExpenseShares;

    public ObservableCollection<SmartAlert> SmartAlerts { get; } = [];
    public ObservableCollection<DailyTaskItem> DailyTasks { get; } = [];

    [ObservableProperty]
    private int _dailyTaskCount;

    [ObservableProperty]
    private int _smartAlertCount;

    // ── Snackbar ───────────────────────────────────────────
    public SnackbarMessageQueue SnackbarQueue { get; } = new(TimeSpan.FromSeconds(3));

    // ── Loading state ──────────────────────────────────────
    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private string _welcomeGreeting = string.Empty;

    [ObservableProperty]
    private string _userDisplayName = string.Empty;

    [ObservableProperty]
    private string _displayDate = string.Empty;

    // ── Summary cards ──────────────────────────────────────
    [ObservableProperty]
    private decimal _todaySales;

    [ObservableProperty]
    private decimal _todayPurchases;

    [ObservableProperty]
    private decimal _netProfit;

    [ObservableProperty]
    private int _overdueInstallmentsCount;

    [ObservableProperty]
    private decimal _investorBalance;

    [ObservableProperty]
    private decimal _unpaidInstallmentsBalance;

    [ObservableProperty]
    private decimal _customerCreditBalance;

    [ObservableProperty]
    private decimal _totalCashBalance;

    // ── Charts ─────────────────────────────────────────────
    [ObservableProperty]
    private ISeries[] _salesSeries = [];

    [ObservableProperty]
    private Axis[] _salesXAxes = [];

    [ObservableProperty]
    private Axis[] _salesYAxes = [];

    [ObservableProperty]
    private ISeries[] _expenseSeries = [];

    // ── Tables ─────────────────────────────────────────────
    public ObservableCollection<RecentTransaction> RecentTransactions { get; } = [];
    public ObservableCollection<UpcomingInstallment> UpcomingInstallments { get; } = [];

    // ── Bottom row ─────────────────────────────────────────
    public ObservableCollection<CashBoxSummary> CashBoxes { get; } = [];

    [ObservableProperty]
    private decimal _bankBalance;

    [ObservableProperty]
    private decimal _totalInventoryValue;

    public DashboardViewModel(IDashboardService dashboardService, ISmartAlertService smartAlertService,
        MainWindowViewModel mainWindow, IUserPreferencesService userPreferences,
        ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _smartAlertService = smartAlertService;
        _mainWindow = mainWindow;
        _userPreferences = userPreferences;
        _currentUserService = currentUserService;
        PageTitle = "لوحة التحكم";
        IsBusy = true;
        IsLoaded = false;
        ApplyDashboardProfile();
        RefreshWelcomeHeader();
        ThemeChartRefresh.Register(RefreshChartsOnlyAsync);
    }

    private void RefreshWelcomeHeader()
    {
        var hour = DateTime.Now.Hour;
        WelcomeGreeting = hour switch
        {
            >= 5 and < 12 => "صباح الخير",
            >= 12 and < 17 => "مساءً طيباً",
            _ => "مساء الخير"
        };

        UserDisplayName = string.IsNullOrWhiteSpace(_currentUserService.Username)
            ? "مستخدم"
            : _currentUserService.Username;

        try
        {
            DisplayDate = DateTime.Now.ToString("dddd، d MMMM yyyy", new CultureInfo("ar-IQ"));
        }
        catch
        {
            DisplayDate = DateTime.Now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }
    }

    [RelayCommand]
    private async Task OpenSalesInvoiceAsync() =>
        await _mainWindow.OpenTabAsync(typeof(SalesInvoiceViewModel), "فاتورة مبيعات", PackIconKind.CashRegister);

    [RelayCommand]
    private async Task OpenPurchaseInvoiceAsync() =>
        await _mainWindow.OpenTabAsync(typeof(PurchaseInvoiceViewModel), "فاتورة مشتريات", PackIconKind.CartArrowDown);

    [RelayCommand]
    private async Task OpenInstallmentInvoiceAsync() =>
        await _mainWindow.OpenTabAsync(typeof(InstallmentInvoiceViewModel), "فاتورة أقساط", PackIconKind.CalendarClock);

    [RelayCommand]
    private async Task ExecuteDailyTaskAsync(DailyTaskItem? task)
    {
        if (task is null) return;
        await _mainWindow.ExecuteDailyTaskAsync(task.Action);
    }

    [RelayCommand]
    private async Task OpenInstallmentsFromAlertAsync() =>
        await _mainWindow.QuickInstallmentsCommand.ExecuteAsync(null);

    [RelayCommand]
    private async Task OpenCollectionDashboardAsync() =>
        await _mainWindow.OpenTabAsync(typeof(CollectionDashboardViewModel), "لوحة التحصيل", PackIconKind.CashMultiple);

    public override async Task InitializeAsync()
    {
        if (_initialized) return;

        ApplyDashboardProfile();

        IsBusy = true;
        IsLoaded = false;

        // Allow the skeleton shimmer to render before loading data.
        await Task.Yield();

        try
        {
            var data = await Task.Run(() => _dashboardService.GetDashboardDataAsync());
            var alertSummary = await _smartAlertService.GetSummaryAsync();

            // Must update UI-bound properties on the dispatcher thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Summary
                TodaySales = data.TodaySales;
                TodayPurchases = data.TodayPurchases;
                NetProfit = data.NetProfit;
                OverdueInstallmentsCount = data.OverdueInstallmentsCount;
                InvestorBalance = data.InvestorBalance;
                UnpaidInstallmentsBalance = data.UnpaidInstallmentsBalance;
                CustomerCreditBalance = data.CustomerCreditBalance;

                _cachedSalesPoints = data.SalesLast30Days;
                _cachedExpenseShares = data.ExpenseDistribution;
                BuildSalesChart(_cachedSalesPoints);
                BuildExpenseChart(_cachedExpenseShares);

                // Tables
                RecentTransactions.Clear();
                foreach (var t in data.RecentTransactions) RecentTransactions.Add(t);

                UpcomingInstallments.Clear();
                foreach (var i in data.UpcomingInstallments) UpcomingInstallments.Add(i);

                // Bottom
                CashBoxes.Clear();
                foreach (var c in data.CashBoxes) CashBoxes.Add(c);
                TotalCashBalance = data.CashBoxes.Sum(c => c.Balance);
                BankBalance = data.BankBalance;
                TotalInventoryValue = data.TotalInventoryValue;

                SmartAlerts.Clear();
                foreach (var a in alertSummary.Alerts)
                    SmartAlerts.Add(a);

                DailyTasks.Clear();
                foreach (var t in alertSummary.DailyTasks)
                    DailyTasks.Add(t);
                DailyTaskCount = alertSummary.TotalTaskCount;
                SmartAlertCount = alertSummary.Alerts.Count;

                IsLoaded = true;
                _initialized = true;
            });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                SnackbarQueue.Enqueue($"⚠ خطأ في تحميل لوحة التحكم: {innerMsg}");
                BeautifulMessageDialog.ShowError(
                    $"خطأ في تحميل لوحة التحكم:\n\n{innerMsg}\n\n{ex.StackTrace}");
                IsLoaded = true;
                _initialized = true;
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task RefreshChartsOnlyAsync()
    {
        if (!_initialized) return Task.CompletedTask;
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_cachedSalesPoints is not null)
                BuildSalesChart(_cachedSalesPoints);
            if (_cachedExpenseShares is not null)
                BuildExpenseChart(_cachedExpenseShares);
        });
        return Task.CompletedTask;
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

    private void BuildExpenseChart(List<ExpenseCategoryShare> shares)
    {
        ExpenseSeries = shares.Count == 0
            ? []
            : ChartThemeConfig.PieFromNameAmount(
                shares.Select(s => new NameAmountPoint { Name = s.Category, Amount = s.Amount }).ToList());
    }
}
