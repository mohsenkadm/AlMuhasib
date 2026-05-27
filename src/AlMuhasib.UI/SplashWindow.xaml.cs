using System.Windows;
using System.Windows.Media.Animation;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        AppNameText.Text = AppInfo.AppNameAr;
        AppNameEnText.Text = AppInfo.AppNameEn;
        VersionText.Text = AppInfo.VersionLabel;
    }

    public void SetStatus(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetStatus(message));
            return;
        }

        StatusText.Text = message;
    }

    public void SetProgress(double fraction)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetProgress(fraction));
            return;
        }

        var trackWidth = ProgressTrack.ActualWidth > 1 ? ProgressTrack.ActualWidth : 400;
        ProgressFill.Width = trackWidth * Math.Clamp(fraction, 0, 1);
    }

    public async Task CloseAnimatedAsync()
    {
        SetStatus("جاهز للاستخدام");
        SetProgress(1);

        var duration = TimeSpan.FromMilliseconds(420);
        var fade = new DoubleAnimation(1, 0, duration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        if (ContentScale.RenderTransform is System.Windows.Media.ScaleTransform scale)
        {
            var shrink = new DoubleAnimation(1, 0.94, duration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, shrink);
            scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, shrink);
        }

        Root.BeginAnimation(OpacityProperty, fade);
        await Task.Delay(450);
        Close();
    }
}
