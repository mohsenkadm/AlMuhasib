using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public partial class NotificationPanel : UserControl
{
    public NotificationPanel()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Vm =>
        DataContext as MainWindowViewModel
        ?? (Window.GetWindow(this)?.DataContext as MainWindowViewModel);

    private void RefreshNotificationsButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateHeaderButton(sender as FrameworkElement);
        Vm?.ReloadNotificationsPanelCommand.Execute(null);
        e.Handled = true;
    }

    private void CloseNotificationsButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateHeaderButton(sender as FrameworkElement);
        Vm?.CloseNotificationPanelCommand.Execute(null);
        e.Handled = true;
    }

    private void HeaderButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // يضمن الاستجابة حتى فوق منطقة عنوان النافذة (WindowChrome)
        AnimateHeaderButton(sender as FrameworkElement);

        if (sender is not FrameworkElement { Name: { } name })
            return;

        if (name == "BtnCloseNotifications")
            Vm?.CloseNotificationPanelCommand.Execute(null);
        else if (name == "BtnRefreshNotifications")
            Vm?.ReloadNotificationsPanelCommand.Execute(null);

        e.Handled = true;
    }

    private static void AnimateHeaderButton(FrameworkElement? element)
    {
        if (element is null)
            return;

        if (element.RenderTransform is not ScaleTransform)
        {
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new ScaleTransform(1, 1);
        }

        var transform = (ScaleTransform)element.RenderTransform;
        var animX = new DoubleAnimation(1, 0.86, TimeSpan.FromMilliseconds(80))
        {
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var animY = new DoubleAnimation(1, 0.86, TimeSpan.FromMilliseconds(80))
        {
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }
}
