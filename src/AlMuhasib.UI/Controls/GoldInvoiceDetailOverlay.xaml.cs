using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AlMuhasib.UI.Controls;

public partial class GoldInvoiceDetailOverlay : UserControl
{
    private Window? _hostWindow;
    private Window? _ownerWindow;

    public GoldInvoiceDetailOverlay()
    {
        InitializeComponent();
    }

    public void ShowCentered()
    {
        _ownerWindow = Window.GetWindow(this) ?? Application.Current.MainWindow;
        if (_ownerWindow is null)
            return;

        _hostWindow = new Window
        {
            Owner = _ownerWindow,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            ResizeMode = ResizeMode.NoResize,
            FlowDirection = FlowDirection.RightToLeft,
            Content = this
        };

        _hostWindow.SourceInitialized += HostWindow_SourceInitialized;
        _ownerWindow.LocationChanged += OwnerWindow_BoundsChanged;
        _ownerWindow.SizeChanged += OwnerWindow_BoundsChanged;
        _ownerWindow.StateChanged += OwnerWindow_BoundsChanged;

        Loaded += Overlay_Loaded;
        _hostWindow.ShowDialog();

        _ownerWindow.LocationChanged -= OwnerWindow_BoundsChanged;
        _ownerWindow.SizeChanged -= OwnerWindow_BoundsChanged;
        _ownerWindow.StateChanged -= OwnerWindow_BoundsChanged;
        _hostWindow.SourceInitialized -= HostWindow_SourceInitialized;
        Loaded -= Overlay_Loaded;
        _hostWindow = null;
        _ownerWindow = null;
    }

    private void HostWindow_SourceInitialized(object? sender, EventArgs e) => SyncToOwnerBounds();

    private void OwnerWindow_BoundsChanged(object? sender, EventArgs e) => SyncToOwnerBounds();

    private void Overlay_Loaded(object sender, RoutedEventArgs e)
    {
        SyncToOwnerBounds();
        PlayOpenAnimation();
    }

    private void SyncToOwnerBounds()
    {
        if (_hostWindow is null || _ownerWindow is null)
            return;

        var topLeft = _ownerWindow.PointToScreen(new Point(0, 0));
        var width = Math.Max(400, _ownerWindow.ActualWidth);
        var height = Math.Max(300, _ownerWindow.ActualHeight);

        _hostWindow.Left = topLeft.X;
        _hostWindow.Top = topLeft.Y;
        _hostWindow.Width = width;
        _hostWindow.Height = height;

        Width = width;
        Height = height;
        RootHost.Width = width;
        RootHost.Height = height;

        DialogCard.Width = Math.Min(920, width - 48);
        DialogCard.MaxHeight = Math.Max(420, height - 48);
    }

    private void PlayOpenAnimation()
    {
        Backdrop.Opacity = 0;
        DialogCard.Opacity = 0;
        if (DialogCard.RenderTransform is ScaleTransform scale)
            scale.ScaleX = scale.ScaleY = 0.92;

        Backdrop.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        DialogCard.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        var grow = new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.28 }
        };
        if (DialogCard.RenderTransform is ScaleTransform st)
        {
            st.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        }
    }

    private void Close()
    {
        if (_hostWindow is null)
            return;

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) => _hostWindow.Close();
        Backdrop.BeginAnimation(OpacityProperty, fade);
        DialogCard.BeginAnimation(OpacityProperty, fade);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == Backdrop)
            Close();
    }
}
