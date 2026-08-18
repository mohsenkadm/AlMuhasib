using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public partial class ProductPickerOverlay : UserControl
{
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(ProductPickerOverlay),
            new PropertyMetadata(false, OnIsOpenChanged));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public ProductPickerOverlay()
    {
        InitializeComponent();
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ProductPickerOverlay overlay)
            return;

        if (e.NewValue is true)
            overlay.OpenPicker();
        else
            overlay.ClosePicker();
    }

    private void OpenPicker()
    {
        var window = Window.GetWindow(this) ?? Application.Current.MainWindow;
        if (window is null)
            return;

        WindowPopup.PlacementTarget = window;
        WindowPopup.Placement = PlacementMode.Relative;
        WindowPopup.HorizontalOffset = 0;
        WindowPopup.VerticalOffset = 0;
        WindowPopup.IsOpen = true;
        PlayOpenAnimation();
    }

    private void ClosePicker()
    {
        if (!WindowPopup.IsOpen)
            return;

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) => WindowPopup.IsOpen = false;
        FullScreenHost.BeginAnimation(OpacityProperty, fade);
    }

    private void WindowPopup_Opened(object sender, EventArgs e)
    {
        var window = WindowPopup.PlacementTarget as Window ?? Application.Current.MainWindow;
        if (window is null)
            return;

        FullScreenHost.Width = window.ActualWidth;
        FullScreenHost.Height = window.ActualHeight;
        FullScreenHost.FlowDirection = FlowDirection.RightToLeft;
        FullScreenHost.Opacity = 0;

        var maxHeight = Math.Max(560, window.ActualHeight * 0.78);
        DialogCard.MaxHeight = maxHeight;
        DialogCard.MinHeight = Math.Min(480, maxHeight * 0.72);
    }

    private void PlayOpenAnimation()
    {
        FullScreenHost.Opacity = 0;
        FullScreenHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        var scale = new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(340))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.32 }
        };
        DialogCard.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scale);
        DialogCard.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scale);
    }

    private void ProductCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as DependencyObject))
            return;

        if (sender is FrameworkElement { DataContext: ProductPickerDisplayItem item }
            && DataContext is ProductPickerViewModel vm)
            vm.AddProductCommand.Execute(item);
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
