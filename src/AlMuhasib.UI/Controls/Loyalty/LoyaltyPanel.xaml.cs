using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Controls.Loyalty;

public partial class LoyaltyPanel : UserControl
{
    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(
            nameof(IsCompact),
            typeof(bool),
            typeof(LoyaltyPanel),
            new PropertyMetadata(false, OnIsCompactChanged));

    public LoyaltyPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += (_, _) => TryAnimateIn();
        IsVisibleChanged += (_, _) => TryAnimateIn();
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoyaltyPanel panel)
            panel.ApplyCompactVisuals();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyCompactVisuals();
        TryAnimateIn();
    }

    private void ApplyCompactVisuals()
    {
        if (PanelRoot?.Effect is DropShadowEffect shadow)
        {
            shadow.BlurRadius = IsCompact ? 10 : 18;
            shadow.Opacity = IsCompact ? 0.18 : 0.28;
            shadow.ShadowDepth = IsCompact ? 2 : 3;
        }
    }

    private void TryAnimateIn()
    {
        if (!IsVisible || PanelRoot is null)
            return;

        // Avoid leaving the panel invisible if animation is skipped
        PanelRoot.Opacity = 0;
        var fromY = IsCompact ? 8.0 : 18.0;
        if (PanelRoot.RenderTransform is TranslateTransform tt)
            tt.Y = fromY;

        PageEntranceAnimator.AnimateFadeSlide(
            PanelRoot,
            delayMs: 40,
            axisY: true,
            from: fromY,
            durationMs: IsCompact ? 260 : 380,
            slideDurationMs: IsCompact ? 280 : 420);
    }
}
