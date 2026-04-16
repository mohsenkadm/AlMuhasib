using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

public partial class AnimatedStatCard : UserControl
{
    private DispatcherTimer? _animTimer;
    private decimal _animCurrent;
    private decimal _animTarget;
    private int _animStep;
    private const int AnimSteps = 20;
    private const int AnimIntervalMs = 40; // 20 steps × 40ms = 800ms total

    public AnimatedStatCard()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateComparison();
        AnimateValue();
    }

    // ── Title ──
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(AnimatedStatCard), new PropertyMetadata(string.Empty));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    // ── Value (the raw decimal) ──
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(decimal), typeof(AnimatedStatCard),
            new PropertyMetadata(0m, OnValueChanged));
    public decimal Value { get => (decimal)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    // ── DisplayValue (formatted string shown in UI) ──
    public static readonly DependencyProperty DisplayValueProperty =
        DependencyProperty.Register(nameof(DisplayValue), typeof(string), typeof(AnimatedStatCard), new PropertyMetadata("0"));
    public string DisplayValue { get => (string)GetValue(DisplayValueProperty); set => SetValue(DisplayValueProperty, value); }

    // ── TextValue (for non-numeric display like dates, names) ──
    public static readonly DependencyProperty TextValueProperty =
        DependencyProperty.Register(nameof(TextValue), typeof(string), typeof(AnimatedStatCard),
            new PropertyMetadata(null, OnTextValueChanged));
    public string? TextValue { get => (string?)GetValue(TextValueProperty); set => SetValue(TextValueProperty, value); }

    // ── Suffix ──
    public static readonly DependencyProperty SuffixProperty =
        DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(AnimatedStatCard), new PropertyMetadata(null));
    public string? Suffix { get => (string?)GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }

    // ── Icon ──
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(PackIconKind), typeof(AnimatedStatCard), new PropertyMetadata(PackIconKind.Information));
    public PackIconKind Icon { get => (PackIconKind)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    // ── IconBackground ──
    public static readonly DependencyProperty IconBackgroundProperty =
        DependencyProperty.Register(nameof(IconBackground), typeof(Brush), typeof(AnimatedStatCard),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD))));
    public Brush IconBackground { get => (Brush)GetValue(IconBackgroundProperty); set => SetValue(IconBackgroundProperty, value); }

    // ── IconForeground ──
    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(nameof(IconForeground), typeof(Brush), typeof(AnimatedStatCard),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0))));
    public Brush IconForeground { get => (Brush)GetValue(IconForegroundProperty); set => SetValue(IconForegroundProperty, value); }

    // ── ValueForeground ──
    public static readonly DependencyProperty ValueForegroundProperty =
        DependencyProperty.Register(nameof(ValueForeground), typeof(Brush), typeof(AnimatedStatCard),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21))));
    public Brush ValueForeground { get => (Brush)GetValue(ValueForegroundProperty); set => SetValue(ValueForegroundProperty, value); }

    // ── ComparisonValue (percentage change) ──
    public static readonly DependencyProperty ComparisonValueProperty =
        DependencyProperty.Register(nameof(ComparisonValue), typeof(decimal?), typeof(AnimatedStatCard),
            new PropertyMetadata(null, OnComparisonChanged));
    public decimal? ComparisonValue { get => (decimal?)GetValue(ComparisonValueProperty); set => SetValue(ComparisonValueProperty, value); }

    // ── ComparisonLabel ──
    public static readonly DependencyProperty ComparisonLabelProperty =
        DependencyProperty.Register(nameof(ComparisonLabel), typeof(string), typeof(AnimatedStatCard),
            new PropertyMetadata(null, OnComparisonChanged));
    public string? ComparisonLabel { get => (string?)GetValue(ComparisonLabelProperty); set => SetValue(ComparisonLabelProperty, value); }

    // ── Callbacks ──

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedStatCard card && card.IsLoaded)
            card.AnimateValue();
    }

    private static void OnTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedStatCard card && e.NewValue is string text)
            card.DisplayValue = text;
    }

    private static void OnComparisonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedStatCard card)
            card.UpdateComparison();
    }

    private void AnimateValue()
    {
        // If TextValue is set, skip numeric animation
        if (TextValue is not null) return;

        _animTimer?.Stop();
        _animTarget = Value;
        _animCurrent = 0;
        _animStep = 0;

        if (_animTarget == 0)
        {
            DisplayValue = "0";
            return;
        }

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AnimIntervalMs) };
        _animTimer.Tick += (_, _) =>
        {
            _animStep++;
            // Ease-out: decelerate towards target
            double t = (double)_animStep / AnimSteps;
            t = 1 - Math.Pow(1 - t, 3); // cubic ease out
            _animCurrent = _animTarget * (decimal)t;

            if (_animStep >= AnimSteps)
            {
                _animCurrent = _animTarget;
                _animTimer.Stop();
            }

            DisplayValue = _animCurrent.ToString("N0");
        };
        _animTimer.Start();
    }

    private void UpdateComparison()
    {
        if (ComparisonValue is null || ComparisonLabel is null)
            return;

        var val = ComparisonValue.Value;
        if (val >= 0)
        {
            ComparisonArrow.Text = $"▲ +{val:N1}%";
            ComparisonArrow.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
        }
        else
        {
            ComparisonArrow.Text = $"▼ {val:N1}%";
            ComparisonArrow.Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
        }
        ComparisonText.Text = ComparisonLabel;
    }
}
