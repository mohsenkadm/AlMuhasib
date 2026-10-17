using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AlMuhasib.UI.ViewModels;

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
        PlayOpenAnimation();
        StartTypingAnimation();
        InputBox.Focus();
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

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
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
