using System.Windows;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Charts;
using LiveChartsCore;

namespace AlMuhasib.UI.Views;

public partial class ProductAnalyticsDetailWindow : Window
{
    public ProductAnalyticsDetailWindow(ProductAnalyticsDetail detail)
    {
        InitializeComponent();
        DataContext = detail;
        var trend = detail.Trend;
        TrendChart.Series = new ISeries[]
        {
            ChartThemeConfig.Line(trend.Select(p => p.SalesAmount).ToArray(), "صافي المبيعات", 0)
        };
        TrendChart.XAxes = [ChartThemeConfig.CreateXAxis(trend.Select(p => p.Label).ToArray(), trend.Count > 10 ? -35 : 0)];
        TrendChart.YAxes = [ChartThemeConfig.CreateYAxis(forceNonNegative: false)];
        BalanceNote.Visibility = string.IsNullOrWhiteSpace(detail.BalanceNote)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
