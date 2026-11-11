using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Windows;

public partial class AiChatWindow : Window
{
    private readonly AiChatViewModel _viewModel;
    private Storyboard? _typingStoryboard;
    private bool _isClosingAnimated;
    private bool _allowClose;
    private bool _userScrolledUp;

    public AiChatWindow(AiChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Loaded += OnLoaded;
        Closing += OnClosing;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.OnChatOpened();
        PlayOpenAnimation();
        StartTypingAnimation();
        StartIconPulse();
        InputBox.Focus();
    }

    private bool _iconPulsing;

    private void StartIconPulse()
    {
        if (_iconPulsing || HeaderOrb.RenderTransform is not ScaleTransform scale)
            return;

        _iconPulsing = true;

        var sb = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        sb.Children.Add(PulseScale(scale, ScaleTransform.ScaleXProperty));
        sb.Children.Add(PulseScale(scale, ScaleTransform.ScaleYProperty));
        sb.Begin();
    }

    private static DoubleAnimation PulseScale(ScaleTransform target, DependencyProperty property)
    {
        var anim = new DoubleAnimation
        {
            From = 1,
            To = 1.08,
            Duration = TimeSpan.FromMilliseconds(900),
            AutoReverse = true,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, new PropertyPath(property));
        return anim;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AiChatViewModel.LastAddedMessage)
            or nameof(AiChatViewModel.IsBusy)
            or nameof(AiChatViewModel.ShowWelcome))
        {
            Dispatcher.BeginInvoke(ScrollToEndIfNeeded, DispatcherPriority.Background);
        }

        if (e.PropertyName == nameof(AiChatViewModel.IsBusy))
        {
            if (_viewModel.IsBusy)
                _typingStoryboard?.Begin(this, true);
            else
                _typingStoryboard?.Stop(this);
        }
    }

    private void ScrollToEndIfNeeded()
    {
        if (_userScrolledUp)
            return;

        MessagesScroll.ScrollToEnd();
    }

    private void PlayOpenAnimation()
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            Opacity = 1;
            return;
        }

        Opacity = 0;
        RootScale.ScaleX = 0.94;
        RootScale.ScaleY = 0.94;
        RootTranslate.Y = 16;

        var sb = new Storyboard();
        sb.Children.Add(CreateDoubleAnim(this, OpacityProperty, 0, 1, 220));
        sb.Children.Add(CreateDoubleAnim(RootScale, ScaleTransform.ScaleXProperty, 0.94, 1, 280, new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }));
        sb.Children.Add(CreateDoubleAnim(RootScale, ScaleTransform.ScaleYProperty, 0.94, 1, 280, new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }));
        sb.Children.Add(CreateDoubleAnim(RootTranslate, TranslateTransform.YProperty, 16, 0, 280, new CubicEase { EasingMode = EasingMode.EaseOut }));
        sb.Begin();
    }

    private void PlayCloseAnimation(Action onCompleted)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            onCompleted();
            return;
        }

        var sb = new Storyboard();
        sb.Children.Add(CreateDoubleAnim(this, OpacityProperty, Opacity, 0, 160));
        sb.Children.Add(CreateDoubleAnim(RootScale, ScaleTransform.ScaleXProperty, RootScale.ScaleX, 0.94, 180));
        sb.Children.Add(CreateDoubleAnim(RootScale, ScaleTransform.ScaleYProperty, RootScale.ScaleY, 0.94, 180));
        sb.Children.Add(CreateDoubleAnim(RootTranslate, TranslateTransform.YProperty, 0, 12, 180));
        sb.Completed += (_, _) => onCompleted();
        sb.Begin();
    }

    private void StartTypingAnimation()
    {
        _typingStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        _typingStoryboard.Children.Add(PulseDot(Dot1, 0));
        _typingStoryboard.Children.Add(PulseDot(Dot2, 140));
        _typingStoryboard.Children.Add(PulseDot(Dot3, 280));
    }

    private static DoubleAnimation PulseDot(FrameworkElement target, int delayMs)
    {
        var anim = new DoubleAnimation
        {
            From = 0.3,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(420),
            AutoReverse = true,
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, new PropertyPath(OpacityProperty));
        return anim;
    }

    private static DoubleAnimation CreateDoubleAnim(
        DependencyObject target,
        DependencyProperty property,
        double from,
        double to,
        int ms,
        IEasingFunction? easing = null)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(ms),
            EasingFunction = easing ?? new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, new PropertyPath(property));
        return anim;
    }

    private Rect _restoreBounds;
    private bool _isExpanded;
    private string? _resizeEdge;
    private Point _resizeOrigin;
    private Rect _resizeStartBounds;

    public bool IsExpanded => _isExpanded;

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        if (e.ClickCount == 2)
        {
            ToggleExpanded();
            return;
        }

        if (_isExpanded)
            RestoreManualSize();

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove throws if the mouse button is no longer down.
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleExpanded();

    private void ToggleExpanded()
    {
        if (_isExpanded)
            RestoreManualSize();
        else
            ExpandToWorkArea();
    }

    private void ExpandToWorkArea()
    {
        _restoreBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        ApplyScreenLimits();
        var area = CurrentWorkArea();
        Left = area.Left;
        Top = area.Top;
        Width = area.Width;
        Height = area.Height;
        _isExpanded = true;
        MaximizeIcon.Kind = PackIconKind.WindowRestore;
        MaximizeButton.ToolTip = "إرجاع القياس السابق";
    }

    private void RestoreManualSize()
    {
        var bounds = _restoreBounds.Width >= MinWidth && _restoreBounds.Height >= MinHeight
            ? _restoreBounds
            : new Rect(Left, Top, 520, 700);
        var area = CurrentWorkArea();
        Width = Math.Clamp(bounds.Width, MinWidth, area.Width);
        Height = Math.Clamp(bounds.Height, MinHeight, area.Height);
        Left = Math.Clamp(bounds.Left, area.Left, Math.Max(area.Left, area.Right - Width));
        Top = Math.Clamp(bounds.Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
        _isExpanded = false;
        MaximizeIcon.Kind = PackIconKind.WindowMaximize;
        MaximizeButton.ToolTip = "تكبير إلى حجم الشاشة";
    }

    private void ApplyScreenLimits()
    {
        var area = CurrentWorkArea();
        MaxWidth = Math.Max(MinWidth, area.Width);
        MaxHeight = Math.Max(MinHeight, area.Height);
        if (Width > MaxWidth)
            Width = MaxWidth;
        if (Height > MaxHeight)
            Height = MaxHeight;
    }

    private Rect CurrentWorkArea()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return SystemParameters.WorkArea;

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
            return SystemParameters.WorkArea;

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
            return SystemParameters.WorkArea;

        var transform = source.CompositionTarget.TransformFromDevice;
        var topLeft = transform.Transform(new Point(info.rcWork.Left, info.rcWork.Top));
        var bottomRight = transform.Transform(new Point(info.rcWork.Right, info.rcWork.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private const uint MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private void ResizeEdge_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement edge || edge.Tag is not string edgeName)
            return;

        _resizeEdge = edgeName;
        _resizeOrigin = ToDeviceIndependent(edge.PointToScreen(e.GetPosition(edge)));
        _resizeStartBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        edge.CaptureMouse();
        e.Handled = true;
    }

    private void ResizeEdge_MouseMove(object sender, MouseEventArgs e)
    {
        if (_resizeEdge is null || e.LeftButton != MouseButtonState.Pressed || sender is not UIElement edge)
            return;

        var current = ToDeviceIndependent(edge.PointToScreen(e.GetPosition(edge)));
        var dx = current.X - _resizeOrigin.X;
        var dy = current.Y - _resizeOrigin.Y;
        var area = CurrentWorkArea();
        var bounds = _resizeStartBounds;

        var left = bounds.Left;
        var top = bounds.Top;
        var width = bounds.Width;
        var height = bounds.Height;

        if (_resizeEdge is "Left" or "TopLeft" or "BottomLeft")
        {
            var nextWidth = Math.Clamp(bounds.Width - dx, MinWidth, area.Width);
            left = bounds.Right - nextWidth;
            width = nextWidth;
        }

        if (_resizeEdge is "Right" or "TopRight" or "BottomRight")
            width = Math.Clamp(bounds.Width + dx, MinWidth, area.Width);

        if (_resizeEdge is "Top" or "TopLeft" or "TopRight")
        {
            var nextHeight = Math.Clamp(bounds.Height - dy, MinHeight, area.Height);
            top = bounds.Bottom - nextHeight;
            height = nextHeight;
        }

        if (_resizeEdge is "Bottom" or "BottomLeft" or "BottomRight")
            height = Math.Clamp(bounds.Height + dy, MinHeight, area.Height);

        left = Math.Clamp(left, area.Left, Math.Max(area.Left, area.Right - width));
        top = Math.Clamp(top, area.Top, Math.Max(area.Top, area.Bottom - height));
        width = Math.Min(width, area.Right - left);
        height = Math.Min(height, area.Bottom - top);

        Left = left;
        Top = top;
        Width = width;
        Height = height;
        if (_isExpanded)
        {
            _isExpanded = false;
            MaximizeIcon.Kind = PackIconKind.WindowMaximize;
            MaximizeButton.ToolTip = "تكبير إلى حجم الشاشة";
        }
    }

    private void ResizeEdge_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement edge)
            edge.ReleaseMouseCapture();
        _resizeEdge = null;
        if (!_isExpanded)
            _restoreBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => RequestClose();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose || _isClosingAnimated)
            return;

        e.Cancel = true;
        RequestClose();
    }

    /// <summary>Hard close used on application shutdown.</summary>
    public void ForceClose()
    {
        _allowClose = true;
        Closing -= OnClosing;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        Close();
    }

    public void RequestClose()
    {
        if (_isClosingAnimated)
            return;

        _isClosingAnimated = true;
        PlayCloseAnimation(() =>
        {
            Hide();
            Opacity = 1;
            RootScale.ScaleX = 1;
            RootScale.ScaleY = 1;
            RootTranslate.Y = 0;
            _isClosingAnimated = false;
        });
    }

    public void Reveal()
    {
        _viewModel.OnChatOpened();

        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        if (!IsVisible)
        {
            Show();
            PlayOpenAnimation();
        }

        Activate();
        InputBox.Focus();
    }

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            return; // allow new line

        e.Handled = true;
        if (_viewModel.SendCommand.CanExecute(null))
            _ = _viewModel.SendCommand.ExecuteAsync(null);
    }

    private void InputBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        InputShell.BorderBrush = TryFindBrush("PrimaryHueMidBrush")
                                 ?? new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0));
        InputShell.BorderThickness = new Thickness(1.8);
    }

    private void InputBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        InputShell.BorderBrush = TryFindBrush("AiComposerFieldBorderBrush")
                                 ?? TryFindBrush("SubtleBorderBrush")
                                 ?? new SolidColorBrush(Color.FromRgb(0xD5, 0xDE, 0xEA));
        InputShell.BorderThickness = new Thickness(1.5);
    }

    private static Brush? TryFindBrush(string key) =>
        Application.Current?.TryFindResource(key) as Brush;

    private Point ToDeviceIndependent(Point devicePoint)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
            return devicePoint;

        return source.CompositionTarget.TransformFromDevice.Transform(devicePoint);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyScreenLimits();
        LocationChanged += (_, _) =>
        {
            if (_resizeEdge is null)
                ApplyScreenLimits();
        };
        MessagesScroll.ScrollChanged += (_, args) =>
        {
            if (args.ExtentHeightChange == 0)
            {
                // User scrolled manually
                var atBottom = MessagesScroll.VerticalOffset >= MessagesScroll.ScrollableHeight - 24;
                _userScrolledUp = !atBottom;
            }
            else if (!_userScrolledUp)
            {
                MessagesScroll.ScrollToEnd();
            }
        };
    }
}
