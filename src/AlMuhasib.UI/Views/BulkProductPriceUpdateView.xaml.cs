using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class BulkProductPriceUpdateView : UserControl
{
    public BulkProductPriceUpdateView() => InitializeComponent();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PageEntranceAnimator.AnimateFadeSlide(HeroCard, 0, true, 16);
        PageEntranceAnimator.AnimateFadeSlide(StatsPanel, 70, true, 14);
        PageEntranceAnimator.AnimateFadeSlide(SettingsPanel, 110, false, 20);
    }
}
