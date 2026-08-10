using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace AlMuhasib.UI.ViewModels;

public partial class PersonProfileViewModel
{
    [ObservableProperty] private string _profitSales = "0";
    [ObservableProperty] private string _profitCost = "0";
    [ObservableProperty] private string _profitNet = "0";
    [ObservableProperty] private string _profitMargin = "0%";
    [ObservableProperty] private string _customerTabSearch = string.Empty;
    [ObservableProperty] private int _customerSelectedTab;

    [ObservableProperty] private ISeries[] _profitChartSeries = [];
    [ObservableProperty] private Axis[] _profitChartXAxes = [];
    [ObservableProperty] private Axis[] _profitChartYAxes = [];

    [ObservableProperty] private ISeries[] _agingChartSeries = [];
    [ObservableProperty] private Axis[] _agingChartXAxes = [];
    [ObservableProperty] private Axis[] _agingChartYAxes = [];

    public ObservableCollection<CustomerProductPurchaseRow> ProductRows { get; } = [];
    public ObservableCollection<CustomerProductPurchaseRow> FilteredProductRows { get; } = [];
    public ObservableCollection<CustomerAgingDetailRow> AgingDetailRows { get; } = [];
    public ObservableCollection<CustomerAgingDetailRow> FilteredAgingDetailRows { get; } = [];
    public ObservableCollection<CustomerFinancialTxnRow> FinancialRows { get; } = [];
    public ObservableCollection<CustomerFinancialTxnRow> FilteredFinancialRows { get; } = [];
    public ObservableCollection<CustomerDueItemRow> DueRows { get; } = [];
    public ObservableCollection<CustomerDueItemRow> FilteredDueRows { get; } = [];
    public ObservableCollection<CustomerAgingBucketRow> AgingBuckets { get; } = [];

    [RelayCommand]
    private void SendDebtReminderWhatsApp()
    {
        if (SelectedPerson is null || !ShowCustomerExtras)
        {
            BeautifulMessageDialog.ShowWarning("اختر عميلاً أولاً");
            return;
        }

        var overdueAging = AgingDetailRows.Where(a => a.DaysOverdue > 0 && a.RemainingAmount > 0).ToList();
        var overdueDues = DueRows.Where(d =>
            d.RemainingAmount > 0 &&
            (d.Status.Contains("متأخر", StringComparison.OrdinalIgnoreCase) ||
             (d.DueDate.HasValue && d.DueDate.Value.Date < DateTime.Today))).ToList();

        decimal sum;
        int count;
        if (overdueAging.Count > 0)
        {
            sum = overdueAging.Sum(a => a.RemainingAmount);
            count = overdueAging.Count;
        }
        else if (overdueDues.Count > 0)
        {
            sum = overdueDues.Sum(d => d.RemainingAmount);
            count = overdueDues.Count;
        }
        else
        {
            BeautifulMessageDialog.ShowWarning("لا توجد مستحقات متأخرة لإرسال تذكير");
            return;
        }

        var name = PersonName;
        var message = string.Join("\n",
            "السلام عليكم،",
            $"السيد/ة {name}،",
            $"نود تذكيركم بوجود {count} مستحق/مستحقات متأخرة بإجمالي {sum:N0} د.ع.",
            "يرجى التسديد في أقرب وقت ممكن.",
            "",
            "مع التحية — المحاسب");

        _whatsAppShare.ShareTextMessage(Phone == "—" ? null : Phone, name, message);
    }

    private void ApplyCustomerInsights(CustomerProfileInsights? insights)
    {
        ProductRows.Clear();
        AgingDetailRows.Clear();
        FinancialRows.Clear();
        DueRows.Clear();
        AgingBuckets.Clear();
        ProfitChartSeries = [];
        AgingChartSeries = [];

        if (insights is null)
        {
            ProfitSales = ProfitCost = ProfitNet = "0";
            ProfitMargin = "0%";
            ApplyCustomerTabFilters();
            return;
        }

        ProfitSales = FormatCurrency(insights.SalesAmount);
        ProfitCost = FormatCurrency(insights.CostAmount);
        ProfitNet = FormatCurrency(insights.NetProfit);
        ProfitMargin = $"{insights.MarginPercent:N2}%";

        foreach (var p in insights.Products) ProductRows.Add(p);
        foreach (var a in insights.AgingDetails) AgingDetailRows.Add(a);
        foreach (var f in insights.FinancialTransactions) FinancialRows.Add(f);
        foreach (var d in insights.DueItems) DueRows.Add(d);
        foreach (var b in insights.AgingBuckets) AgingBuckets.Add(b);

        BuildProfitChart(insights.ProfitByMonth);
        BuildAgingChart(insights.AgingBuckets);
        ApplyCustomerTabFilters();
    }

    partial void OnCustomerTabSearchChanged(string value) => ApplyCustomerTabFilters();

    private void ApplyCustomerTabFilters()
    {
        var term = CustomerTabSearch?.Trim();
        FilterInto(FilteredProductRows, ProductRows, p =>
            Matches(term, p.ProductName));
        FilterInto(FilteredAgingDetailRows, AgingDetailRows, a =>
            Matches(term, a.SourceType, a.Reference, a.AgingBucket));
        FilterInto(FilteredFinancialRows, FinancialRows, f =>
            Matches(term, f.VoucherNumber, f.VoucherType, f.Notes));
        FilterInto(FilteredDueRows, DueRows, d =>
            Matches(term, d.Kind, d.Title, d.Subtitle, d.Status));
    }

    private static void FilterInto<T>(ObservableCollection<T> target, ObservableCollection<T> source, Func<T, bool> predicate)
    {
        target.Clear();
        foreach (var item in source.Where(predicate))
            target.Add(item);
    }

    private static bool Matches(string? term, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(term)) return true;
        return values.Any(v => !string.IsNullOrWhiteSpace(v) &&
                               v.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private void BuildProfitChart(List<CustomerProfitMonthPoint> points)
    {
        if (points.Count == 0)
        {
            ProfitChartSeries = [];
            ProfitChartXAxes = [];
            ProfitChartYAxes = [];
            return;
        }

        ProfitChartSeries =
        [
            ChartThemeConfig.Column(points.Select(p => p.Sales).ToArray(), "المبيعات", 0),
            ChartThemeConfig.Column(points.Select(p => p.Profit).ToArray(), "صافي الربح", 2)
        ];
        ProfitChartXAxes =
        [
            new Axis
            {
                Labels = points.Select(p => p.Label).ToArray(),
                LabelsRotation = 15,
                LabelsPaint = new SolidColorPaint(ChartThemeConfig.LabelColor)
            }
        ];
        ProfitChartYAxes =
        [
            new Axis
            {
                LabelsPaint = new SolidColorPaint(ChartThemeConfig.LabelColor),
                SeparatorsPaint = new SolidColorPaint(ChartThemeConfig.GridLineColor) { StrokeThickness = 1 }
            }
        ];
    }

    private void BuildAgingChart(List<CustomerAgingBucketRow> buckets)
    {
        if (buckets.Count == 0)
        {
            AgingChartSeries = [];
            AgingChartXAxes = [];
            AgingChartYAxes = [];
            return;
        }

        AgingChartSeries =
        [
            ChartThemeConfig.Column(buckets.Select(b => b.Amount).ToArray(), "المبلغ", 3)
        ];
        AgingChartXAxes =
        [
            new Axis
            {
                Labels = buckets.Select(b => b.BucketName).ToArray(),
                LabelsRotation = 10,
                LabelsPaint = new SolidColorPaint(ChartThemeConfig.LabelColor)
            }
        ];
        AgingChartYAxes =
        [
            new Axis
            {
                LabelsPaint = new SolidColorPaint(ChartThemeConfig.LabelColor),
                SeparatorsPaint = new SolidColorPaint(ChartThemeConfig.GridLineColor) { StrokeThickness = 1 }
            }
        ];
    }
}
