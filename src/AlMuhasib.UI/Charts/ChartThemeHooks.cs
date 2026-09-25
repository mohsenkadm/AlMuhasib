using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AlMuhasib.UI.Services;
using LiveChartsCore.SkiaSharpView.WPF;

namespace AlMuhasib.UI.Charts;

/// <summary>
/// Applies LiveCharts tooltip/legend paints and themed chrome on load and theme toggle.
/// Charts self-register weakly — never walks the full visual tree (that froze the UI).
/// </summary>
public static class ChartThemeHooks
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<Control>> Charts = [];
    private static bool _initialized;
    private static CancellationTokenSource? _debounceCts;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        EventManager.RegisterClassHandler(typeof(CartesianChart), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, _) => OnChartLoaded((Control)s)));
        EventManager.RegisterClassHandler(typeof(PieChart), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, _) => OnChartLoaded((Control)s)));
        EventManager.RegisterClassHandler(typeof(CartesianChart), FrameworkElement.UnloadedEvent,
            new RoutedEventHandler((s, _) => Unregister((Control)s)));
        EventManager.RegisterClassHandler(typeof(PieChart), FrameworkElement.UnloadedEvent,
            new RoutedEventHandler((s, _) => Unregister((Control)s)));

        ThemeService.ThemeChanged += (_, _) => ScheduleThemeApply();
    }

    private static void OnChartLoaded(Control chart)
    {
        Register(chart);
        Apply(chart);
    }

    private static void Register(Control chart)
    {
        lock (Gate)
        {
            Prune_NoLock();
            foreach (var weak in Charts)
            {
                if (weak.TryGetTarget(out var existing) && ReferenceEquals(existing, chart))
                    return;
            }
            Charts.Add(new WeakReference<Control>(chart));
        }
    }

    private static void Unregister(Control chart)
    {
        lock (Gate)
        {
            Charts.RemoveAll(w =>
                !w.TryGetTarget(out var existing) || ReferenceEquals(existing, chart));
        }
    }

    private static void Prune_NoLock()
        => Charts.RemoveAll(w => !w.TryGetTarget(out _));

    private static async void ScheduleThemeApply()
    {
        var cts = new CancellationTokenSource();
        lock (Gate)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = cts;
        }

        try
        {
            await Task.Delay(80, cts.Token).ConfigureAwait(true);
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null) return;

            Control[] live;
            lock (Gate)
            {
                Prune_NoLock();
                live = Charts
                    .Select(w => w.TryGetTarget(out var c) ? c : null)
                    .Where(c => c is not null)
                    .Cast<Control>()
                    .ToArray();
            }

            ChartThemeConfig.EnsurePaints();

            // Batch apply off the critical theme-switch path.
            await dispatcher.InvokeAsync(() =>
            {
                foreach (var chart in live)
                {
                    if (chart.IsLoaded)
                        Apply(chart);
                }
            }, DispatcherPriority.Background);
        }
        catch (TaskCanceledException)
        {
            // newer theme toggle
        }
    }

    private static void Apply(Control chart)
    {
        ChartThemeConfig.EnsurePaints();

        // Transparent so the card/host surface shows through (avoids nested boxes)
        chart.Background = Brushes.Transparent;
        chart.BorderBrush = Brushes.Transparent;
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
