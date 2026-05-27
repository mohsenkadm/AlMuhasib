using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AlMuhasib.UI.Views;

public partial class PrintLayoutSettingsView
{
    public PrintLayoutSettingsView() => InitializeComponent();

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        AnimateFadeSlide(HeroHeader, 0, axisY: true, from: 16);
        AnimateFadeSlide(SettingsColumn, 90, axisY: false, from: -24);
        AnimateFadeSlide(PreviewCard, 150, axisY: false, from: 24);
    }

    private static void AnimateFadeSlide(UIElement target, int delayMs, bool axisY, double from)
    {
        target.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(450))
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        if (target.RenderTransform is not TranslateTransform tt)
            return;

        var anim = new DoubleAnimation(from, 0, TimeSpan.FromMilliseconds(500))
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        if (axisY)
            tt.BeginAnimation(TranslateTransform.YProperty, anim);
        else
            tt.BeginAnimation(TranslateTransform.XProperty, anim);
    }
}
