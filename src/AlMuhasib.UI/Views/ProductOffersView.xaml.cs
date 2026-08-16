using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class ProductOffersView : UserControl
{
    public ProductOffersView() => InitializeComponent();

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        PageEntranceAnimator.AnimateFadeSlide(HeroHeader, 0, true, 16);
        PageEntranceAnimator.AnimateFadeSlide(ToolbarCard, 80, true, 14);
        PageEntranceAnimator.AnimateFadeSlide(ListCard, 140, true, 14);
    }
}
