using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

public partial class AnimatedStatCard : UserControl
{
    private EventHandler? _renderHandler;
    private long _animStartTicks;
    private decimal _animTarget;
    private const double AnimDurationMs = 300; // سريع جداً مثل عدّادات الويب

    public AnimatedStatCard()
    {
        InitializeComponent();
        if (ReadLocalValue(IconBackgroundProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(IconBackgroundProperty, "PrimaryHueLightBrush");
        if (ReadLocalValue(IconForegroundProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(IconForegroundProperty, "PrimaryHueMidBrush");
        if (ReadLocalValue(ValueForegroundProperty) == DependencyProperty.UnsetValue)
            SetResourceReference(ValueForegroundProperty, "TextPrimaryBrush");
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateComparison();
        AnimateValue();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => StopAnimation();

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

    // ── Hint (short explanation under the title) ──
    public static readonly DependencyProperty HintProperty =
        DependencyProperty.Register(nameof(Hint), typeof(string), typeof(AnimatedStatCard), new PropertyMetadata(null));
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

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

    // ── Detail button ──
    public static readonly DependencyProperty ShowDetailButtonProperty =
        DependencyProperty.Register(nameof(ShowDetailButton), typeof(bool), typeof(AnimatedStatCard),
            new PropertyMetadata(false));
    public bool ShowDetailButton { get => (bool)GetValue(ShowDetailButtonProperty); set => SetValue(ShowDetailButtonProperty, value); }

    public static readonly DependencyProperty DetailCommandProperty =
        DependencyProperty.Register(nameof(DetailCommand), typeof(ICommand), typeof(AnimatedStatCard),
            new PropertyMetadata(null));
    public ICommand? DetailCommand { get => (ICommand?)GetValue(DetailCommandProperty); set => SetValue(DetailCommandProperty, value); }

    public static readonly DependencyProperty DetailCommandParameterProperty =
        DependencyProperty.Register(nameof(DetailCommandParameter), typeof(object), typeof(AnimatedStatCard),
            new PropertyMetadata(null));
    public object? DetailCommandParameter { get => GetValue(DetailCommandParameterProperty); set => SetValue(DetailCommandParameterProperty, value); }

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

        StopAnimation();
        _animTarget = Value;

        if (_animTarget == 0)
        {
            DisplayValue = FormatAnimated(0);
            return;
        }

        DisplayValue = FormatAnimated(0);
        _animStartTicks = Environment.TickCount64;
        _renderHandler = OnRenderingFrame;
        CompositionTarget.Rendering += _renderHandler;
    }

    private void OnRenderingFrame(object? sender, EventArgs e)
    {
        var elapsed = Environment.TickCount64 - _animStartTicks;
        var t = Math.Clamp(elapsed / AnimDurationMs, 0.0, 1.0);
        // تخفيف خفيف فقط — بدون بطء ملحوظ قرب النهاية (أفضل من cubic للأرصدة الكبيرة)
        var eased = 1.0 - Math.Pow(1.0 - t, 1.5);
        DisplayValue = FormatAnimated(_animTarget * (decimal)eased);

        if (t >= 1.0)
        {
            DisplayValue = FormatAnimated(_animTarget);
            StopAnimation();
        }
    }

    private void StopAnimation()
    {
        if (_renderHandler is null) return;
        CompositionTarget.Rendering -= _renderHandler;
        _renderHandler = null;
    }

    private string FormatAnimated(decimal value)
        => string.Equals(Suffix?.Trim(), "%", StringComparison.Ordinal)
            ? value.ToString("N1")
            : value.ToString("N0");

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
