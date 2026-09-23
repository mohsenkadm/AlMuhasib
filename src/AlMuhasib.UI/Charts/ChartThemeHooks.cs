using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlMuhasib.UI.Services;
using LiveChartsCore.SkiaSharpView.WPF;

namespace AlMuhasib.UI.Charts;

/// <summary>
/// Applies LiveCharts tooltip/legend paints and themed chrome on load and theme toggle.
/// </summary>
public static class ChartThemeHooks
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        EventManager.RegisterClassHandler(typeof(CartesianChart), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, _) => Apply((Control)s)));
        EventManager.RegisterClassHandler(typeof(PieChart), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, _) => Apply((Control)s)));

        ThemeService.ThemeChanged += (_, _) =>
        {
            if (Application.Current?.MainWindow is null) return;
            ApplyToVisualTree(Application.Current.MainWindow);
        };
    }

    private static void ApplyToVisualTree(DependencyObject root)
    {
        if (root is CartesianChart cart)
            Apply(cart);
        else if (root is PieChart pie)
            Apply(pie);

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            ApplyToVisualTree(VisualTreeHelper.GetChild(root, i));
    }

    private static void Apply(Control chart)
    {
        ChartThemeConfig.EnsurePaints();

        // Kill default white LiveCharts surface — bind to themed chart well
        chart.SetResourceReference(Control.BackgroundProperty, "DashboardChartWellBrush");
        chart.SetResourceReference(Control.BorderBrushProperty, "DashboardChartWellBorderBrush");
        chart.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");
        chart.BorderThickness = new Thickness(0);

        switch (chart)
        {
            case CartesianChart cart:
                cart.TooltipBackgroundPaint = ChartThemeConfig.TooltipBackgroundPaint;
                cart.TooltipTextPaint = ChartThemeConfig.TooltipTextPaint;
                cart.LegendTextPaint = ChartThemeConfig.LegendTextPaint;
                break;
            case PieChart pie:
                pie.TooltipBackgroundPaint = ChartThemeConfig.TooltipBackgroundPaint;
                pie.TooltipTextPaint = ChartThemeConfig.TooltipTextPaint;
                pie.LegendTextPaint = ChartThemeConfig.LegendTextPaint;
                break;
        }
    }
}
