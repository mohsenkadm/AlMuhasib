using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using AlMuhasib.Core.Enums;
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
using AlMuhasib.UI.Models;
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
    private decimal _netProfitSales;

    [ObservableProperty]
    private decimal _netProfitPurchases;

    [ObservableProperty]
    private decimal _netProfitOpeningStock;

    [ObservableProperty]
    private decimal _netProfitExpenses;

    [ObservableProperty]
    private decimal _netProfitDistributions;

    [ObservableProperty]
    private decimal _netProfitOpening;

    /// <summary>فواتير المشتريات + الرصيد الافتتاحي للمخزون (للعرض في تفاصيل المصدر).</summary>
    public decimal NetProfitPurchasesWithOpeningStock =>
        NetProfitPurchases + NetProfitOpeningStock;

    public bool ShowOpeningStockPurchasesHint => NetProfitOpeningStock > 0;

    public string OpeningStockPurchasesHint =>
        NetProfitOpeningStock > 0
            ? $"رصيد افتتاحي للمخزون: {NetProfitOpeningStock:N0} د.ع (يُخصم في الأرباح)"
            : string.Empty;

    [ObservableProperty]
    private int _overdueInstallmentsCount;

    [ObservableProperty]
    private decimal _investorBalance;

    [ObservableProperty]
    private decimal _investorOpeningTotal;

    [ObservableProperty]
    private decimal _investorDepositsTotal;

    [ObservableProperty]
    private decimal _investorWithdrawalsTotal;

    [ObservableProperty]
    private decimal _unpaidInstallmentsBalance;

    [ObservableProperty]
    private decimal _customerCreditBalance;

    [ObservableProperty]
    private decimal _customerCreditInvoiceRemaining;

    [ObservableProperty]
    private decimal _customerCreditUnappliedDebt;

    [ObservableProperty]
    private decimal _customerCreditUnappliedReceipts;

    [ObservableProperty]
    private decimal _supplierCreditBalance;

    [ObservableProperty]
    private decimal _supplierCreditInvoiceRemaining;

    [ObservableProperty]
    private decimal _supplierCreditUnappliedPayments;

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

    // ── KPI sparkline values (last 14 days) ────────────────
    public ObservableCollection<decimal> TodaySalesChartValues { get; } = [];
    public ObservableCollection<decimal> TodayPurchasesChartValues { get; } = [];
    public ObservableCollection<decimal> NetProfitChartValues { get; } = [];
    public ObservableCollection<decimal> OverdueInstallmentsChartValues { get; } = [];
    public ObservableCollection<decimal> InvestorBalanceChartValues { get; } = [];
    public ObservableCollection<decimal> UnpaidInstallmentsChartValues { get; } = [];
    public ObservableCollection<decimal> CustomerCreditChartValues { get; } = [];
    public ObservableCollection<decimal> SupplierCreditChartValues { get; } = [];
    public ObservableCollection<decimal> CashBalanceChartValues { get; } = [];
    public ObservableCollection<decimal> BankBalanceChartValues { get; } = [];
    public ObservableCollection<decimal> InventoryValueChartValues { get; } = [];

    [ObservableProperty] private decimal? _todaySalesTrendPercent;
    [ObservableProperty] private decimal? _todayPurchasesTrendPercent;
    [ObservableProperty] private decimal? _netProfitTrendPercent;
    [ObservableProperty] private decimal? _overdueInstallmentsTrendPercent;
    [ObservableProperty] private decimal? _investorBalanceTrendPercent;
    [ObservableProperty] private decimal? _unpaidInstallmentsTrendPercent;
    [ObservableProperty] private decimal? _customerCreditTrendPercent;
    [ObservableProperty] private decimal? _supplierCreditTrendPercent;
    [ObservableProperty] private decimal? _cashBalanceTrendPercent;
    [ObservableProperty] private decimal? _bankBalanceTrendPercent;
    [ObservableProperty] private decimal? _inventoryValueTrendPercent;

    private List<DailySalesPoint>? _cachedPurchasesPoints;
    private List<DailySalesPoint>? _cachedNetProfitPoints;
    private List<DailySalesPoint>? _cachedOverduePoints;
    private List<DailySalesPoint>? _cachedInvestorPoints;
    private List<DailySalesPoint>? _cachedUnpaidPoints;
    private List<DailySalesPoint>? _cachedCustomerCreditPoints;
    private List<DailySalesPoint>? _cachedSupplierCreditPoints;
    private List<DailySalesPoint>? _cachedCashFlowPoints;
    private List<DailySalesPoint>? _cachedBankFlowPoints;
    private List<DailySalesPoint>? _cachedInventoryPoints;

    public DashboardViewModel(IDashboardService dashboardService, ISmartAlertService smartAlertService,
        MainWindowViewModel mainWindow, ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _smartAlertService = smartAlertService;
        _mainWindow = mainWindow;
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
    private void ShowNetProfitDetails()
    {
        AmountBreakdownDialog.Show(new AmountBreakdownModel
        {
            Title = "تفاصيل الأرباح الصافية",
            Subtitle = "معادلة لوحة التحكم (من بداية النشاط حتى الآن)",
            Formula = "الصافي = صافي المبيعات − صافي المشتريات − الرصيد الافتتاحي للمخزون − المصاريف − التوزيعات + رصيد افتتاحي للأرباح",
            ResultLabel = "الأرباح الصافية",
            ResultAmount = NetProfit,
            Lines =
            [
                new AmountBreakdownLine
                {
                    Operator = "+",
                    Label = "صافي المبيعات",
                    Amount = NetProfitSales,
                    Description = "فواتير البيع والأقساط − مرتجعات المبيعات"
                },
                new AmountBreakdownLine
                {
                    Operator = "−",
                    Label = "صافي المشتريات",
                    Amount = NetProfitPurchases,
                    Description = "فواتير المشتريات − مرتجعات المشتريات"
                },
                new AmountBreakdownLine
                {
                    Operator = "−",
                    Label = "رصيد افتتاحي للمخزون",
                    Amount = NetProfitOpeningStock,
                    Description = "قيمة أرصدة المنتجات الافتتاحية (الكمية × تكلفة الوحدة)"
                },
                new AmountBreakdownLine
                {
                    Operator = "−",
                    Label = "إجمالي المصاريف",
                    Amount = NetProfitExpenses
                },
                new AmountBreakdownLine
                {
                    Operator = "−",
                    Label = "توزيعات الأرباح",
                    Amount = NetProfitDistributions
                },
                new AmountBreakdownLine
                {
                    Operator = "+",
                    Label = "رصيد افتتاحي للأرباح",
                    Amount = NetProfitOpening
                },
                new AmountBreakdownLine
                {
                    Operator = "=",
                    Label = "الأرباح الصافية",
                    Amount = NetProfit,
                    IsResult = true
                }
            ],
            Note = "فواتير المشتريات والرصيد الافتتاحي للمخزون يُخصمان معاً في الأرباح. اضغط أيقونة التفاصيل على بطاقة المشتريات لمعرفة مصدر المبلغ."
        });
    }

    [RelayCommand]
    private void ShowPurchasesSourceDetails()
    {
        AmountBreakdownDialog.Show(new AmountBreakdownModel
        {
            Title = "تفاصيل مصدر المشتريات",
            Subtitle = "من أين جاء المبلغ المستخدم في معادلة الأرباح",
            Formula = "إجمالي تكلفة التوريد = فواتير المشتريات + الرصيد الافتتاحي للمخزون",
            ResultLabel = "إجمالي تكلفة التوريد",
            ResultAmount = NetProfitPurchasesWithOpeningStock,
            Lines =
            [
                new AmountBreakdownLine
                {
                    Operator = "+",
                    Label = "فواتير المشتريات",
                    Amount = NetProfitPurchases,
                    Description = "مجموع فواتير المشتريات من بداية النشاط"
                },
                new AmountBreakdownLine
                {
                    Operator = "+",
                    Label = "رصيد افتتاحي للمخزون",
                    Amount = NetProfitOpeningStock,
                    Description = "قيمة أرصدة المنتجات الافتتاحية (الكمية × تكلفة الوحدة)"
                },
                new AmountBreakdownLine
                {
                    Operator = "=",
                    Label = "إجمالي تكلفة التوريد",
                    Amount = NetProfitPurchasesWithOpeningStock,
                    IsResult = true
                }
            ],
            Note = "بطاقة «المشتريات اليوم» تعرض فواتير اليوم فقط. الرصيد الافتتاحي يُحسب ضمن الأرباح الصافية وليس ضمن مشتريات اليوم."
        });
    }

    [RelayCommand]
    private void ShowCustomerCreditDetails()
    {
        AmountBreakdownDialog.Show(new AmountBreakdownModel
        {
            Title = "تفاصيل رصيد الآجل للعملاء",
            Subtitle = "معادلة لوحة التحكم (من بداية النشاط حتى الآن)",
            Formula = "الرصيد = متبقي فواتير الآجل − سندات دين غير مطبّقة − سندات قبض غير مطبّقة",
            ResultLabel = "رصيد الآجل للعملاء",
            ResultAmount = CustomerCreditBalance,
            Lines =
            [
                new AmountBreakdownLine
                {
                    Operator = "+",
                    Label = "متبقي فواتير آجل العملاء",
                    Amount = CustomerCreditInvoiceRemaining,
                    Description = "فواتير المبيعات والأقساط الآجلة غير المسددة"
                },
                new AmountBreakdownLine
                {
                    Operator = "−",
                    Label = "سندات قبض دين غير مطبّقة",
                    Amount = CustomerCreditUnappliedDebt,
                    Description = "سندات تسديد دين لم تُطبَّق بعد على الفواتير"
                },
                new AmountBreakdownLine
                {
                    Operator = "−",
                    Label = "سندات قبض غير مطبّقة",
                    Amount = CustomerCreditUnappliedReceipts,
                    Description = "سندات قبض عامة (بدون فاتورة/قسط) لم تُطبَّق على فواتير الآجل"
                },
                new AmountBreakdownLine
                {
                    Operator = "=",
                    Label = "رصيد الآجل للعملاء",
                    Amount = CustomerCreditBalance,
                    IsResult = true
                }
            ],
            Note = "يشمل فواتير المبيعات والأقساط فقط — فواتير مشتريات الموردين تظهر في بطاقة آجل الموردين."
        });
    }

    [RelayCommand]
    private void ShowSupplierCreditDetails()
    {
        AmountBreakdownDialog.Show(new AmountBreakdownModel
        {
            Title = "تفاصيل رصيد الآجل للموردين",
            Subtitle = "معادلة لوحة التحكم (من بداية النشاط حتى الآن)",
            Formula = "الرصيد = متبقي فواتير مشتريات الآجل − سندات صرف غير مطبّقة",
            ResultLabel = "رصيد الآجل للموردين",
            ResultAmount = SupplierCreditBalance,
            Lines =
            [
                new AmountBreakdownLine
                {
                    Operator = "+",
                    Label = "متبقي فواتير مشتريات الآجل",
                    Amount = SupplierCreditInvoiceRemaining,
                    Description = "مجموع RemainingAmount لفواتير الشراء الآجلة غير المسددة"
                },
                new AmountBreakdownLine
                {
                    Operator = "−",
                    Label = "سندات صرف غير مطبّقة",
                    Amount = SupplierCreditUnappliedPayments,
                    Description = "سندات صرف لموردين لم تُطبَّق بعد على فواتير الشراء"
                },
                new AmountBreakdownLine
                {
                    Operator = "=",
                    Label = "رصيد الآجل للموردين",
                    Amount = SupplierCreditBalance,
                    IsResult = true
                }
            ],
            Note = "سند الصرف المرتبط بمورد يُطبَّق تلقائياً على فواتير الشراء الآجلة الأقدم أولاً."
        });
    }

    [RelayCommand]
    private void ShowInvestorBalanceDetails()
    {
        AmountBreakdownDialog.Show(new AmountBreakdownModel
        {
            Title = "تفاصيل رصيد المستثمرين",
            Subtitle = "معادلة لوحة التحكم (من بداية النشاط حتى الآن)",
            Formula = "الرصيد = أرصدة افتتاحية + إيداعات − سحوبات",
            ResultLabel = "رصيد المستثمرين",
            ResultAmount = InvestorBalance,
            Lines =
            [
                new AmountBreakdownLine
                {
                    Operator = "+",
                    Label = "أرصدة افتتاحية",
                    Amount = InvestorOpeningTotal
                },
                new AmountBreakdownLine
                {
                    Operator = "+",
                    Label = "إجمالي الإيداعات",
                    Amount = InvestorDepositsTotal
                },
                new AmountBreakdownLine
                {
                    Operator = "−",
                    Label = "إجمالي السحوبات",
                    Amount = InvestorWithdrawalsTotal
                },
                new AmountBreakdownLine
                {
                    Operator = "=",
                    Label = "رصيد المستثمرين",
                    Amount = InvestorBalance,
                    IsResult = true
                }
            ],
            Note = "الرصيد المعروض هو مجموع TotalDeposit لكل مستثمر. توزيعات الأرباح لا تغيّر رصيد الإيداع."
        });
    }

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

    [RelayCommand]
    private async Task RefreshDashboardAsync()
    {
        if (IsBusy) return;
        _initialized = false;
        RefreshWelcomeHeader();
        await InitializeAsync();
    }

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
                NetProfitSales = data.NetProfitSales;
                NetProfitPurchases = data.NetProfitPurchases;
                NetProfitOpeningStock = data.NetProfitOpeningStock;
                NetProfitExpenses = data.NetProfitExpenses;
                NetProfitDistributions = data.NetProfitDistributions;
                NetProfitOpening = data.NetProfitOpening;
                OnPropertyChanged(nameof(NetProfitPurchasesWithOpeningStock));
                OnPropertyChanged(nameof(ShowOpeningStockPurchasesHint));
                OnPropertyChanged(nameof(OpeningStockPurchasesHint));
                OverdueInstallmentsCount = data.OverdueInstallmentsCount;
                InvestorBalance = data.InvestorBalance;
                InvestorOpeningTotal = data.InvestorOpeningTotal;
                InvestorDepositsTotal = data.InvestorDepositsTotal;
                InvestorWithdrawalsTotal = data.InvestorWithdrawalsTotal;
                UnpaidInstallmentsBalance = data.UnpaidInstallmentsBalance;
                CustomerCreditBalance = data.CustomerCreditBalance;
                CustomerCreditInvoiceRemaining = data.CustomerCreditInvoiceRemaining;
                CustomerCreditUnappliedDebt = data.CustomerCreditUnappliedDebt;
                CustomerCreditUnappliedReceipts = data.CustomerCreditUnappliedReceipts;
                SupplierCreditBalance = data.SupplierCreditBalance;
                SupplierCreditInvoiceRemaining = data.SupplierCreditInvoiceRemaining;
                SupplierCreditUnappliedPayments = data.SupplierCreditUnappliedPayments;

                _cachedSalesPoints = data.SalesLast30Days;
                _cachedExpenseShares = data.ExpenseDistribution;
                _cachedPurchasesPoints = data.PurchasesLast14Days;
                _cachedNetProfitPoints = data.NetProfitLast14Days;
                _cachedOverduePoints = data.OverdueInstallmentsLast14Days;
                _cachedInvestorPoints = data.InvestorBalanceLast14Days;
                _cachedUnpaidPoints = data.UnpaidInstallmentsLast14Days;
                _cachedCustomerCreditPoints = data.CustomerCreditLast14Days;
                _cachedSupplierCreditPoints = data.SupplierCreditLast14Days;
                _cachedCashFlowPoints = data.CashFlowLast14Days;
                _cachedBankFlowPoints = data.BankFlowLast14Days;
                _cachedInventoryPoints = data.InventoryValueLast14Days;

                BuildSalesChart(_cachedSalesPoints);
                BuildExpenseChart(_cachedExpenseShares);
                ApplyKpiSparklines(data);

                // Tables
                RecentTransactions.Clear();
                foreach (var t in data.RecentTransactions) RecentTransactions.Add(t);

                UpcomingInstallments.Clear();
                foreach (var i in data.UpcomingInstallments) UpcomingInstallments.Add(i);

                // Bottom
                CashBoxes.Clear();
                foreach (var c in data.CashBoxes) CashBoxes.Add(c);
                TotalCashBalance = data.CashBalanceIqd != 0 || data.CashBalanceUsd != 0
                    ? data.CashBalanceIqd
                    : data.CashBoxes.Where(c => c.Currency == AccountingCurrency.IQD).Sum(c => c.Balance);
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

        // Already scheduled on the UI dispatcher by ThemeChartRefresh — never nest Dispatcher.Invoke.
        if (_cachedSalesPoints is not null)
            BuildSalesChart(_cachedSalesPoints);
        if (_cachedExpenseShares is not null)
            BuildExpenseChart(_cachedExpenseShares);
        RefreshSparklineCollections();
        return Task.CompletedTask;
    }

    private void ApplyKpiSparklines(DashboardData data)
    {
        ReplaceChartValues(TodaySalesChartValues, TakeLast14(_cachedSalesPoints));
        ReplaceChartValues(TodayPurchasesChartValues, data.PurchasesLast14Days);
        ReplaceChartValues(NetProfitChartValues, data.NetProfitLast14Days);
        ReplaceChartValues(OverdueInstallmentsChartValues, data.OverdueInstallmentsLast14Days);
        ReplaceChartValues(InvestorBalanceChartValues, data.InvestorBalanceLast14Days);
        ReplaceChartValues(UnpaidInstallmentsChartValues, data.UnpaidInstallmentsLast14Days);
        ReplaceChartValues(CustomerCreditChartValues, data.CustomerCreditLast14Days);
        ReplaceChartValues(SupplierCreditChartValues, data.SupplierCreditLast14Days);
        ReplaceChartValues(CashBalanceChartValues, data.CashFlowLast14Days);
        ReplaceChartValues(BankBalanceChartValues, data.BankFlowLast14Days);
        ReplaceChartValues(InventoryValueChartValues, data.InventoryValueLast14Days);

        TodaySalesTrendPercent = data.TodaySalesTrendPercent;
        TodayPurchasesTrendPercent = data.TodayPurchasesTrendPercent;
        NetProfitTrendPercent = data.NetProfitTrendPercent;
        OverdueInstallmentsTrendPercent = data.OverdueInstallmentsTrendPercent;
        InvestorBalanceTrendPercent = data.InvestorBalanceTrendPercent;
        UnpaidInstallmentsTrendPercent = data.UnpaidInstallmentsTrendPercent;
        CustomerCreditTrendPercent = data.CustomerCreditTrendPercent;
        SupplierCreditTrendPercent = data.SupplierCreditTrendPercent;
        CashBalanceTrendPercent = data.CashBalanceTrendPercent;
        BankBalanceTrendPercent = data.BankBalanceTrendPercent;
        InventoryValueTrendPercent = data.InventoryValueTrendPercent;
    }

    private void RefreshSparklineCollections()
    {
        ReplaceChartValues(TodaySalesChartValues, TakeLast14(_cachedSalesPoints));
        ReplaceChartValues(TodayPurchasesChartValues, _cachedPurchasesPoints);
        ReplaceChartValues(NetProfitChartValues, _cachedNetProfitPoints);
        ReplaceChartValues(OverdueInstallmentsChartValues, _cachedOverduePoints);
        ReplaceChartValues(InvestorBalanceChartValues, _cachedInvestorPoints);
        ReplaceChartValues(UnpaidInstallmentsChartValues, _cachedUnpaidPoints);
        ReplaceChartValues(CustomerCreditChartValues, _cachedCustomerCreditPoints);
        ReplaceChartValues(SupplierCreditChartValues, _cachedSupplierCreditPoints);
        ReplaceChartValues(CashBalanceChartValues, _cachedCashFlowPoints);
        ReplaceChartValues(BankBalanceChartValues, _cachedBankFlowPoints);
        ReplaceChartValues(InventoryValueChartValues, _cachedInventoryPoints);
    }

    private static List<DailySalesPoint> TakeLast14(List<DailySalesPoint>? points)
    {
        if (points is null || points.Count == 0) return [];
        return points.Count <= 14 ? points : points.Skip(points.Count - 14).ToList();
    }

    private static void ReplaceChartValues(ObservableCollection<decimal> target, IEnumerable<DailySalesPoint>? points)
    {
        target.Clear();
        if (points is null) return;
        foreach (var p in points)
            target.Add(p.Amount);
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
