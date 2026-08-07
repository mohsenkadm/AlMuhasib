using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AlMuhasib.UI.Views;

public partial class PermissionsView
{
    public PermissionsView()
    {
        InitializeComponent();
    }

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        AnimateFadeSlide(HeroHeader, 0, fromY: 18);
        AnimateFadeSlide(ToolbarCard, 80, fromY: 12);
        AnimateFadeSlide(GridCard, 140, fromY: 16);
    }

    private static void AnimateFadeSlide(UIElement target, int delayMs, double fromY)
    {
        target.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420))
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        if (target.RenderTransform is not TranslateTransform tt)
            return;

        tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(fromY, 0, TimeSpan.FromMilliseconds(480))
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }
}
