using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MaterialDesignThemes.Wpf;
using SkiaSharp;

namespace AlMuhasib.UI.Controls;

[ContentProperty(nameof(DetailsContent))]
public partial class DashboardKpiCard : UserControl
{
    private static readonly Axis[] DefaultSparkX = [ChartThemeConfig.CreateSparklineAxis()];
    private static readonly Axis[] DefaultSparkY = [ChartThemeConfig.CreateSparklineAxis(isY: true)];
    private INotifyCollectionChanged? _chartValuesNotify;

    public DashboardKpiCard()
    {
        InitializeComponent();
        if (ReadLocalValue(AccentBrushProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(AccentBrushProperty, "PrimaryHueMidBrush");
        if (ReadLocalValue(AccentLightBrushProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(AccentLightBrushProperty, "PrimaryHueLightBrush");
        if (ReadLocalValue(ValueBrushProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(ValueBrushProperty, "TextPrimaryBrush");

        MiniChartXAxes = DefaultSparkX;
        MiniChartYAxes = DefaultSparkY;
        Loaded += (_, _) =>
        {
            RebuildSparkline();
            UpdateTrendVisuals();
        };
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(DashboardKpiCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(DashboardKpiCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(PackIconKind), typeof(DashboardKpiCard),
            new PropertyMetadata(PackIconKind.ChartLine));

    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(DashboardKpiCard),
            new PropertyMetadata(Brushes.SteelBlue, OnChartVisualChanged));

    public static readonly DependencyProperty AccentLightBrushProperty =
        DependencyProperty.Register(nameof(AccentLightBrush), typeof(Brush), typeof(DashboardKpiCard),
            new PropertyMetadata(Brushes.AliceBlue));

    public static readonly DependencyProperty ValueBrushProperty =
        DependencyProperty.Register(nameof(ValueBrush), typeof(Brush), typeof(DashboardKpiCard),
            new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty DetailsContentProperty =
        DependencyProperty.Register(nameof(DetailsContent), typeof(object), typeof(DashboardKpiCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ShowDetailButtonProperty =
        DependencyProperty.Register(nameof(ShowDetailButton), typeof(bool), typeof(DashboardKpiCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty DetailCommandProperty =
        DependencyProperty.Register(nameof(DetailCommand), typeof(ICommand), typeof(DashboardKpiCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ChartValuesProperty =
        DependencyProperty.Register(nameof(ChartValues), typeof(IEnumerable), typeof(DashboardKpiCard),
            new PropertyMetadata(null, OnChartValuesChanged));

    public static readonly DependencyProperty TrendPercentProperty =
        DependencyProperty.Register(nameof(TrendPercent), typeof(decimal?), typeof(DashboardKpiCard),
            new PropertyMetadata(null, OnTrendChanged));

    public static readonly DependencyProperty ShowMiniChartProperty =
        DependencyProperty.Register(nameof(ShowMiniChart), typeof(bool), typeof(DashboardKpiCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty MiniChartSeriesProperty =
        DependencyProperty.Register(nameof(MiniChartSeries), typeof(ISeries[]), typeof(DashboardKpiCard),
            new PropertyMetadata(Array.Empty<ISeries>()));

    public static readonly DependencyProperty MiniChartXAxesProperty =
        DependencyProperty.Register(nameof(MiniChartXAxes), typeof(Axis[]), typeof(DashboardKpiCard),
            new PropertyMetadata(DefaultSparkX));

    public static readonly DependencyProperty MiniChartYAxesProperty =
        DependencyProperty.Register(nameof(MiniChartYAxes), typeof(Axis[]), typeof(DashboardKpiCard),
            new PropertyMetadata(DefaultSparkY));

    public static readonly DependencyProperty ShowTrendProperty =
        DependencyProperty.Register(nameof(ShowTrend), typeof(bool), typeof(DashboardKpiCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty TrendTextProperty =
        DependencyProperty.Register(nameof(TrendText), typeof(string), typeof(DashboardKpiCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TrendBrushProperty =
        DependencyProperty.Register(nameof(TrendBrush), typeof(Brush), typeof(DashboardKpiCard),
            new PropertyMetadata(Brushes.Gray));

    public static readonly DependencyProperty TrendIconProperty =
        DependencyProperty.Register(nameof(TrendIcon), typeof(PackIconKind), typeof(DashboardKpiCard),
            new PropertyMetadata(PackIconKind.Minus));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public PackIconKind Icon
    {
        get => (PackIconKind)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush AccentLightBrush
    {
        get => (Brush)GetValue(AccentLightBrushProperty);
        set => SetValue(AccentLightBrushProperty, value);
    }

    public Brush ValueBrush
    {
        get => (Brush)GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    public object? DetailsContent
    {
        get => GetValue(DetailsContentProperty);
        set => SetValue(DetailsContentProperty, value);
    }

    public bool ShowDetailButton
    {
        get => (bool)GetValue(ShowDetailButtonProperty);
        set => SetValue(ShowDetailButtonProperty, value);
    }

    public ICommand? DetailCommand
    {
        get => (ICommand?)GetValue(DetailCommandProperty);
        set => SetValue(DetailCommandProperty, value);
    }

    public IEnumerable? ChartValues
    {
        get => (IEnumerable?)GetValue(ChartValuesProperty);
        set => SetValue(ChartValuesProperty, value);
    }

    public decimal? TrendPercent
    {
        get => (decimal?)GetValue(TrendPercentProperty);
        set => SetValue(TrendPercentProperty, value);
    }

    public bool ShowMiniChart
    {
        get => (bool)GetValue(ShowMiniChartProperty);
        set => SetValue(ShowMiniChartProperty, value);
    }

    public ISeries[] MiniChartSeries
    {
        get => (ISeries[])GetValue(MiniChartSeriesProperty);
        set => SetValue(MiniChartSeriesProperty, value);
    }

    public Axis[] MiniChartXAxes
    {
        get => (Axis[])GetValue(MiniChartXAxesProperty);
        set => SetValue(MiniChartXAxesProperty, value);
    }

    public Axis[] MiniChartYAxes
    {
        get => (Axis[])GetValue(MiniChartYAxesProperty);
        set => SetValue(MiniChartYAxesProperty, value);
    }

    public bool ShowTrend
    {
        get => (bool)GetValue(ShowTrendProperty);
        set => SetValue(ShowTrendProperty, value);
    }

    public string TrendText
    {
        get => (string)GetValue(TrendTextProperty);
        set => SetValue(TrendTextProperty, value);
    }

    public Brush TrendBrush
    {
        get => (Brush)GetValue(TrendBrushProperty);
        set => SetValue(TrendBrushProperty, value);
    }

    public PackIconKind TrendIcon
    {
        get => (PackIconKind)GetValue(TrendIconProperty);
        set => SetValue(TrendIconProperty, value);
    }

    private static void OnChartValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DashboardKpiCard card) return;
        card.DetachChartValuesNotify();
        if (e.NewValue is INotifyCollectionChanged notify)
        {
            card._chartValuesNotify = notify;
            notify.CollectionChanged += card.OnChartValuesCollectionChanged;
        }
        card.RebuildSparkline();
    }

    private static void OnChartVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DashboardKpiCard card)
            card.RebuildSparkline();
    }

    private static void OnTrendChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DashboardKpiCard card)
            card.UpdateTrendVisuals();
    }

    private void OnChartValuesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildSparkline();

    private void DetachChartValuesNotify()
    {
        if (_chartValuesNotify is null) return;
        _chartValuesNotify.CollectionChanged -= OnChartValuesCollectionChanged;
        _chartValuesNotify = null;
    }

    private void RebuildSparkline()
    {
        var values = ExtractDecimals(ChartValues);
        if (values.Count == 0)
        {
            MiniChartSeries = [];
            ShowMiniChart = false;
            return;
        }

        var accent = ChartThemeConfig.BrushToSkColor(AccentBrush, SKColor.Parse("#1565C0"));
        MiniChartSeries = [ChartThemeConfig.Sparkline(values.ToArray(), accent)];
        MiniChartXAxes = [ChartThemeConfig.CreateSparklineAxis()];
        MiniChartYAxes = [ChartThemeConfig.CreateSparklineAxis(isY: true)];
        ShowMiniChart = true;
    }

    private static List<decimal> ExtractDecimals(IEnumerable? source)
    {
        var list = new List<decimal>();
        if (source is null) return list;
        foreach (var item in source)
        {
            if (item is decimal d) list.Add(d);
            else if (item is double dbl) list.Add((decimal)dbl);
            else if (item is float f) list.Add((decimal)f);
            else if (item is int i) list.Add(i);
            else if (item is long l) list.Add(l);
        }
        return list;
    }

    private void UpdateTrendVisuals()
    {
        if (TrendPercent is not { } pct)
        {
            ShowTrend = false;
            TrendText = string.Empty;
            return;
        }

        ShowTrend = true;
        var abs = Math.Abs(pct);
        var sign = pct > 0 ? "+" : pct < 0 ? "−" : "";
        TrendText = $"{sign}{abs:0.#}%";

        if (pct > 0)
        {
            TrendIcon = PackIconKind.TrendingUp;
            TrendBrush = TryFindResource("DashboardKpiGreenBrush") as Brush ?? Brushes.ForestGreen;
        }
        else if (pct < 0)
        {
            TrendIcon = PackIconKind.TrendingDown;
            TrendBrush = TryFindResource("DashboardKpiRedBrush") as Brush ?? Brushes.IndianRed;
        }
        else
        {
            TrendIcon = PackIconKind.Minus;
            TrendBrush = TryFindResource("HintForegroundBrush") as Brush ?? Brushes.Gray;
        }
    }

    /// <summary>Rebuild sparkline paints after theme toggle.</summary>
    public void RefreshThemeCharts() => RebuildSparkline();
}
