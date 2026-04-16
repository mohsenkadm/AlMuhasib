using System.Windows;
using System.Windows.Controls;

namespace AlMuhasib.UI.Controls;

public partial class ChartCard : UserControl
{
    public ChartCard() => InitializeComponent();

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ChartCard), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(ChartCard), new PropertyMetadata(null));

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly DependencyProperty ChartContentProperty =
        DependencyProperty.Register(nameof(ChartContent), typeof(object), typeof(ChartCard), new PropertyMetadata(null));

    public object? ChartContent
    {
        get => GetValue(ChartContentProperty);
        set => SetValue(ChartContentProperty, value);
    }

    public static readonly DependencyProperty IsEmptyProperty =
        DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(ChartCard), new PropertyMetadata(false));

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    public static readonly DependencyProperty ShowPeriodSelectorProperty =
        DependencyProperty.Register(nameof(ShowPeriodSelector), typeof(bool), typeof(ChartCard), new PropertyMetadata(false));

    public bool ShowPeriodSelector
    {
        get => (bool)GetValue(ShowPeriodSelectorProperty);
        set => SetValue(ShowPeriodSelectorProperty, value);
    }

    public static readonly DependencyProperty IsDaySelectedProperty =
        DependencyProperty.Register(nameof(IsDaySelected), typeof(bool), typeof(ChartCard), new PropertyMetadata(false));

    public bool IsDaySelected
    {
        get => (bool)GetValue(IsDaySelectedProperty);
        set => SetValue(IsDaySelectedProperty, value);
    }

    public static readonly DependencyProperty IsWeekSelectedProperty =
        DependencyProperty.Register(nameof(IsWeekSelected), typeof(bool), typeof(ChartCard), new PropertyMetadata(false));

    public bool IsWeekSelected
    {
        get => (bool)GetValue(IsWeekSelectedProperty);
        set => SetValue(IsWeekSelectedProperty, value);
    }

    public static readonly DependencyProperty IsMonthSelectedProperty =
        DependencyProperty.Register(nameof(IsMonthSelected), typeof(bool), typeof(ChartCard), new PropertyMetadata(true));

    public bool IsMonthSelected
    {
        get => (bool)GetValue(IsMonthSelectedProperty);
        set => SetValue(IsMonthSelectedProperty, value);
    }

    public static readonly DependencyProperty IsYearSelectedProperty =
        DependencyProperty.Register(nameof(IsYearSelected), typeof(bool), typeof(ChartCard), new PropertyMetadata(false));

    public bool IsYearSelected
    {
        get => (bool)GetValue(IsYearSelectedProperty);
        set => SetValue(IsYearSelectedProperty, value);
    }
}
