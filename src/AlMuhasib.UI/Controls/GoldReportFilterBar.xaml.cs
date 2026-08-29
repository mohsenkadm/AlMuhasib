using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Controls;

public partial class GoldReportFilterBar : UserControl
{
    public static readonly DependencyProperty DateFromProperty =
        DependencyProperty.Register(nameof(DateFrom), typeof(DateTime?), typeof(GoldReportFilterBar));

    public static readonly DependencyProperty DateToProperty =
        DependencyProperty.Register(nameof(DateTo), typeof(DateTime?), typeof(GoldReportFilterBar));

    public static readonly DependencyProperty FilterContentProperty =
        DependencyProperty.Register(nameof(FilterContent), typeof(object), typeof(GoldReportFilterBar));

    public static readonly DependencyProperty SearchCommandProperty =
        DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(GoldReportFilterBar));

    public static readonly DependencyProperty SetDateTodayCommandProperty =
        DependencyProperty.Register(nameof(SetDateTodayCommand), typeof(ICommand), typeof(GoldReportFilterBar));

    public static readonly DependencyProperty SetDateWeekCommandProperty =
        DependencyProperty.Register(nameof(SetDateWeekCommand), typeof(ICommand), typeof(GoldReportFilterBar));

    public static readonly DependencyProperty SetDateMonthCommandProperty =
        DependencyProperty.Register(nameof(SetDateMonthCommand), typeof(ICommand), typeof(GoldReportFilterBar));

    public static readonly DependencyProperty SetDateQuarterCommandProperty =
        DependencyProperty.Register(nameof(SetDateQuarterCommand), typeof(ICommand), typeof(GoldReportFilterBar));

    public static readonly DependencyProperty ResetDateFiltersCommandProperty =
        DependencyProperty.Register(nameof(ResetDateFiltersCommand), typeof(ICommand), typeof(GoldReportFilterBar));

    public DateTime? DateFrom
    {
        get => (DateTime?)GetValue(DateFromProperty);
        set => SetValue(DateFromProperty, value);
    }

    public DateTime? DateTo
    {
        get => (DateTime?)GetValue(DateToProperty);
        set => SetValue(DateToProperty, value);
    }

    public object? FilterContent
    {
        get => GetValue(FilterContentProperty);
        set => SetValue(FilterContentProperty, value);
    }

    public ICommand? SearchCommand
    {
        get => (ICommand?)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public ICommand? SetDateTodayCommand
    {
        get => (ICommand?)GetValue(SetDateTodayCommandProperty);
        set => SetValue(SetDateTodayCommandProperty, value);
    }

    public ICommand? SetDateWeekCommand
    {
        get => (ICommand?)GetValue(SetDateWeekCommandProperty);
        set => SetValue(SetDateWeekCommandProperty, value);
    }

    public ICommand? SetDateMonthCommand
    {
        get => (ICommand?)GetValue(SetDateMonthCommandProperty);
        set => SetValue(SetDateMonthCommandProperty, value);
    }

    public ICommand? SetDateQuarterCommand
    {
        get => (ICommand?)GetValue(SetDateQuarterCommandProperty);
        set => SetValue(SetDateQuarterCommandProperty, value);
    }

    public ICommand? ResetDateFiltersCommand
    {
        get => (ICommand?)GetValue(ResetDateFiltersCommandProperty);
        set => SetValue(ResetDateFiltersCommandProperty, value);
    }

    public GoldReportFilterBar() => InitializeComponent();
}
