using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

public partial class AnalyticsMetricCard : UserControl
{
    public AnalyticsMetricCard()
    {
        InitializeComponent();
        if (ReadLocalValue(IconBackgroundProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(IconBackgroundProperty, "DashboardKpiBlueLightBrush");
        if (ReadLocalValue(IconForegroundProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(IconForegroundProperty, "TextPrimaryBrush");
        if (ReadLocalValue(ValueForegroundProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(ValueForegroundProperty, "TextPrimaryBrush");
        if (ReadLocalValue(AccentBrushProperty) == DependencyProperty.UnsetValue)
            AccentBrush = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(AnalyticsMetricCard), new PropertyMetadata(string.Empty));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public static readonly DependencyProperty TextValueProperty =
        DependencyProperty.Register(nameof(TextValue), typeof(string), typeof(AnalyticsMetricCard), new PropertyMetadata("—"));
    public string? TextValue { get => (string?)GetValue(TextValueProperty); set => SetValue(TextValueProperty, value); }

    public static readonly DependencyProperty HintProperty =
        DependencyProperty.Register(nameof(Hint), typeof(string), typeof(AnalyticsMetricCard), new PropertyMetadata(null));
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(PackIconKind), typeof(AnalyticsMetricCard), new PropertyMetadata(PackIconKind.ChartBox));
    public PackIconKind Icon { get => (PackIconKind)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public static readonly DependencyProperty IconBackgroundProperty =
        DependencyProperty.Register(nameof(IconBackground), typeof(Brush), typeof(AnalyticsMetricCard));
    public Brush IconBackground { get => (Brush)GetValue(IconBackgroundProperty); set => SetValue(IconBackgroundProperty, value); }

    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(nameof(IconForeground), typeof(Brush), typeof(AnalyticsMetricCard));
    public Brush IconForeground { get => (Brush)GetValue(IconForegroundProperty); set => SetValue(IconForegroundProperty, value); }

    public static readonly DependencyProperty ValueForegroundProperty =
        DependencyProperty.Register(nameof(ValueForeground), typeof(Brush), typeof(AnalyticsMetricCard));
    public Brush ValueForeground { get => (Brush)GetValue(ValueForegroundProperty); set => SetValue(ValueForegroundProperty, value); }

    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(AnalyticsMetricCard));
    public Brush AccentBrush { get => (Brush)GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }

    public static readonly DependencyProperty ValueFontSizeProperty =
        DependencyProperty.Register(nameof(ValueFontSize), typeof(double), typeof(AnalyticsMetricCard), new PropertyMetadata(22d));
    public double ValueFontSize { get => (double)GetValue(ValueFontSizeProperty); set => SetValue(ValueFontSizeProperty, value); }

    public static readonly DependencyProperty ComparisonValueProperty =
        DependencyProperty.Register(nameof(ComparisonValue), typeof(decimal?), typeof(AnalyticsMetricCard),
            new PropertyMetadata(null, OnComparisonChanged));
    public decimal? ComparisonValue { get => (decimal?)GetValue(ComparisonValueProperty); set => SetValue(ComparisonValueProperty, value); }

    public static readonly DependencyProperty ComparisonLabelProperty =
        DependencyProperty.Register(nameof(ComparisonLabel), typeof(string), typeof(AnalyticsMetricCard),
            new PropertyMetadata(null, OnComparisonChanged));
    public string? ComparisonLabel { get => (string?)GetValue(ComparisonLabelProperty); set => SetValue(ComparisonLabelProperty, value); }

    private static void OnComparisonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnalyticsMetricCard card)
            card.UpdateComparison();
    }

    private void UpdateComparison()
    {
        if (ComparisonValue is null || string.IsNullOrWhiteSpace(ComparisonLabel))
        {
            ComparisonChip.Visibility = Visibility.Collapsed;
            return;
        }

        var value = ComparisonValue.Value;
        ComparisonChip.Visibility = Visibility.Visible;
        if (value >= 0)
        {
            ComparisonText.Text = $"▲ +{value:N1}%  {ComparisonLabel}";
            ComparisonText.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
        }
        else
        {
            ComparisonText.Text = $"▼ {value:N1}%  {ComparisonLabel}";
            ComparisonText.Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
        }
    }
}
