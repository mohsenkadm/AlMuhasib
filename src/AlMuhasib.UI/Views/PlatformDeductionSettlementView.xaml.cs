using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class PlatformDeductionSettlementView : UserControl
{
    public PlatformDeductionSettlementView() => InitializeComponent();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PageEntranceAnimator.AnimateFadeSlide(HeroCard, 0, true, 16);
        PageEntranceAnimator.AnimateFadeSlide(StatsPanel, 80, true, 14);
        PageEntranceAnimator.AnimateFadeSlide(FooterBar, 140, true, 18);
    }
}
