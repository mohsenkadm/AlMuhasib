using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Controls.Loyalty;

public partial class LoyaltyPanel : UserControl
{
    public LoyaltyPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += (_, _) => TryAnimateIn();
        IsVisibleChanged += (_, _) => TryAnimateIn();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => TryAnimateIn();

    private void TryAnimateIn()
    {
        if (!IsVisible || PanelRoot is null)
            return;

        PanelRoot.Opacity = 0;
        if (PanelRoot.RenderTransform is TranslateTransform tt)
            tt.Y = 18;

        PageEntranceAnimator.AnimateFadeSlide(PanelRoot, delayMs: 40, axisY: true, from: 18, durationMs: 380, slideDurationMs: 420);
    }
}
