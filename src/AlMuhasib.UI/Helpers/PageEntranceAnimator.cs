using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AlMuhasib.UI.Helpers;

public static class PageEntranceAnimator
{
    public static void AnimateFadeSlide(UIElement target, int delayMs, bool axisY, double from,
        int durationMs = 450, int slideDurationMs = 500)
    {
        target.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs))
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        if (target.RenderTransform is not TranslateTransform tt)
            return;

        var anim = new DoubleAnimation(from, 0, TimeSpan.FromMilliseconds(slideDurationMs))
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        if (axisY)
            tt.BeginAnimation(TranslateTransform.YProperty, anim);
        else
            tt.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    public static void AnimateStepTransition(UIElement target, bool slideFromRight = true)
    {
        if (target.RenderTransform is not TranslateTransform tt)
            return;

        var fromX = slideFromRight ? 36.0 : -36.0;
        tt.X = fromX;
        target.Opacity = 0;

        target.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        tt.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(fromX, 0, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    public static void StaggerChildren(IEnumerable<UIElement> elements, int startDelayMs = 0, int staggerMs = 80)
    {
        var delay = startDelayMs;
        foreach (var el in elements)
        {
            AnimateFadeSlide(el, delay, axisY: true, from: 14);
            delay += staggerMs;
        }
    }
}
