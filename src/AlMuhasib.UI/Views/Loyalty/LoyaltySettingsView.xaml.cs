using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views.Loyalty;

public partial class LoyaltySettingsView : UserControl
{
    public LoyaltySettingsView() => InitializeComponent();

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        PageEntranceAnimator.AnimateFadeSlide(Hero, 0, true, 16);
        PageEntranceAnimator.AnimateFadeSlide(SectionEarn, 80, true, 14);
        PageEntranceAnimator.AnimateFadeSlide(SectionRedeem, 140, true, 14);

        if (DataContext is ViewModels.LoyaltySettingsViewModel vm)
            FeatureStateText.Text = vm.FeatureEnabled ? "الميزة مفعّلة" : "فعّل الميزة من إعدادات الميزات";
    }
}
