using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MaterialDesignThemes.Wpf;
using SkiaSharp;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;

    // ── Snackbar ───────────────────────────────────────────
    public SnackbarMessageQueue SnackbarQueue { get; } = new(TimeSpan.FromSeconds(3));

    // ── Loading state ──────────────────────────────────────
    [ObservableProperty]
    private bool _isLoaded;

    // ── Summary cards ──────────────────────────────────────
    [ObservableProperty]
    private decimal _todaySales;

    [ObservableProperty]
    private decimal _todayPurchases;

    [ObservableProperty]
    private decimal _netProfit;

    [ObservableProperty]
    private int _overdueInstallmentsCount;

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

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        PageTitle = "لوحة التحكم";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsLoaded = false;

        try
        {
            var data = await Task.Run(() => _dashboardService.GetDashboardDataAsync());

            // Must update UI-bound properties on the dispatcher thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Summary
                TodaySales = data.TodaySales;
                TodayPurchases = data.TodayPurchases;
                NetProfit = data.NetProfit;
                OverdueInstallmentsCount = data.OverdueInstallmentsCount;

                // Sales chart
                BuildSalesChart(data.SalesLast30Days);

                // Expense pie chart
                BuildExpenseChart(data.ExpenseDistribution);

                // Tables
                RecentTransactions.Clear();
                foreach (var t in data.RecentTransactions) RecentTransactions.Add(t);

                UpcomingInstallments.Clear();
                foreach (var i in data.UpcomingInstallments) UpcomingInstallments.Add(i);

                // Bottom
                CashBoxes.Clear();
                foreach (var c in data.CashBoxes) CashBoxes.Add(c);
                BankBalance = data.BankBalance;
                TotalInventoryValue = data.TotalInventoryValue;

                IsLoaded = true;
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
                IsLoaded = true; // Show UI even on error, with default/zero values
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildSalesChart(List<DailySalesPoint> points)
    {
        var values = points.Select(p => new DateTimePoint(p.Date, (double)p.Amount)).ToArray();

        SalesSeries =
        [
            new LineSeries<DateTimePoint>
            {
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse("#1A237E").WithAlpha(30)),
                Stroke = new SolidColorPaint(SKColor.Parse("#1A237E"), 2.5f),
                GeometryFill = new SolidColorPaint(SKColor.Parse("#1A237E")),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#FFFFFF"), 2),
                GeometrySize = 8,
                LineSmoothness = 0.3
            }
        ];

        SalesXAxes =
        [
            new Axis
            {
                Labeler = v => v >= DateTime.MinValue.Ticks && v <= DateTime.MaxValue.Ticks
                    ? new DateTime((long)v).ToString("MM/dd")
                    : string.Empty,
                UnitWidth = TimeSpan.FromDays(1).Ticks,
                MinStep = TimeSpan.FromDays(5).Ticks,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#757575"))
            }
        ];

        SalesYAxes =
        [
            new Axis
            {
                Labeler = v => v.ToString("N0"),
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#757575"))
            }
        ];
    }

    private void BuildExpenseChart(List<ExpenseCategoryShare> shares)
    {
        var colors = new[]
        {
            "#1A237E", "#283593", "#3949AB", "#5C6BC0",
            "#7986CB", "#9FA8DA", "#C5CAE9", "#E8EAF6"
        };

        ExpenseSeries = shares.Select((s, i) => new PieSeries<double>
        {
            Values = [(double)s.Amount],
            Name = s.Category,
            Fill = new SolidColorPaint(SKColor.Parse(colors[i % colors.Length])),
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
            DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#424242")),
            DataLabelsSize = 11
        } as ISeries).ToArray();
    }
}
