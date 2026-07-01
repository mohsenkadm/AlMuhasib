using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

[ContentProperty(nameof(DetailsContent))]
public partial class DashboardKpiCard : UserControl
{
    public DashboardKpiCard()
    {
        InitializeComponent();
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
            new PropertyMetadata(Brushes.SteelBlue));

    public static readonly DependencyProperty AccentLightBrushProperty =
        DependencyProperty.Register(nameof(AccentLightBrush), typeof(Brush), typeof(DashboardKpiCard),
            new PropertyMetadata(Brushes.AliceBlue));

    public static readonly DependencyProperty ValueBrushProperty =
        DependencyProperty.Register(nameof(ValueBrush), typeof(Brush), typeof(DashboardKpiCard),
            new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty DetailsContentProperty =
        DependencyProperty.Register(nameof(DetailsContent), typeof(object), typeof(DashboardKpiCard),
            new PropertyMetadata(null));

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
}
