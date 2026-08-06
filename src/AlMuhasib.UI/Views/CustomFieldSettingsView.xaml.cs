using System.Windows;
using System.Windows.Media.Animation;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class CustomFieldSettingsView
{
    public CustomFieldSettingsView() => InitializeComponent();

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        PageEntranceAnimator.AnimateFadeSlide(HeroHeader, 0, axisY: true, from: 16);
        PageEntranceAnimator.AnimateFadeSlide(TabsBar, 70, axisY: true, from: 14);
        PageEntranceAnimator.AnimateFadeSlide(FieldsPanel, 140, axisY: false, from: 24);
        PageEntranceAnimator.AnimateFadeSlide(SavePanel, 220, axisY: true, from: 14);

        // Soft pulse on fields panel when tab content appears
        FieldsPanel.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420))
        {
            BeginTime = TimeSpan.FromMilliseconds(140),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }
}
