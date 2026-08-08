using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private bool _isNumericPadAnimating;

    /// <summary>When true, the user must authenticate or the application exits.</summary>
    public bool IsSessionLockMode { get; set; }

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;
        _viewModel.StepChanged += OnStepChanged;
        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnWindowLoaded;
        CloseButton.Visibility = IsSessionLockMode ? Visibility.Collapsed : Visibility.Visible;
        if (IsSessionLockMode)
            Title = "انتهت الجلسة — تسجيل الدخول";
        await _viewModel.LoadAdminsAsync();
    }

    private void OnStepChanged()
    {
        CloseNumericPad(animate: false);

        if (_viewModel.IsEnteringPassword)
            AnimateToPasswordStep();
        else
            AnimateToAdminStep();
    }

    private void AnimateToPasswordStep()
    {
        AdminStepPanel.Visibility = Visibility.Visible;
        PasswordStepPanel.Visibility = Visibility.Visible;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var slideOut = new DoubleAnimation(0, -24, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (_, _) => AdminStepPanel.Visibility = Visibility.Collapsed;

        AdminStepPanel.BeginAnimation(OpacityProperty, fadeOut);
        if (AdminStepPanel.RenderTransform is TranslateTransform adminTransform)
            adminTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);

        PasswordStepPanel.Opacity = 0;
        if (PasswordStepPanel.RenderTransform is TranslateTransform passTransform)
            passTransform.X = 36;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            BeginTime = TimeSpan.FromMilliseconds(120),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slideIn = new DoubleAnimation(36, 0, TimeSpan.FromMilliseconds(320))
        {
            BeginTime = TimeSpan.FromMilliseconds(120),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        slideIn.Completed += (_, _) => PasswordBoxHidden.Focus();

        PasswordStepPanel.BeginAnimation(OpacityProperty, fadeIn);
        if (PasswordStepPanel.RenderTransform is TranslateTransform passTransform2)
            passTransform2.BeginAnimation(TranslateTransform.XProperty, slideIn);
    }

    private void AnimateToAdminStep()
    {
        PasswordStepPanel.Visibility = Visibility.Visible;
        AdminStepPanel.Visibility = Visibility.Visible;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) => PasswordStepPanel.Visibility = Visibility.Collapsed;

        PasswordStepPanel.BeginAnimation(OpacityProperty, fadeOut);

        AdminStepPanel.Opacity = 0;
        var adminTransform = AdminStepPanel.RenderTransform as TranslateTransform;
        if (adminTransform != null)
            adminTransform.X = -24;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
        {
            BeginTime = TimeSpan.FromMilliseconds(90),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slideIn = new DoubleAnimation(-24, 0, TimeSpan.FromMilliseconds(280))
        {
            BeginTime = TimeSpan.FromMilliseconds(90),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        AdminStepPanel.BeginAnimation(OpacityProperty, fadeIn);
        adminTransform?.BeginAnimation(TranslateTransform.XProperty, slideIn);
    }

    private void OnLoginSucceeded()
    {
        DialogResult = true;
        Close();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
            _viewModel.Password = pb.Password;
    }

    private void PasswordToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (PasswordToggle.IsChecked == true)
        {
            PasswordBoxVisible.Text = PasswordBoxHidden.Password;
            PasswordBoxHidden.Visibility = Visibility.Collapsed;
            PasswordBoxVisible.Visibility = Visibility.Visible;
            PasswordBoxVisible.Focus();
            PasswordBoxVisible.CaretIndex = PasswordBoxVisible.Text.Length;
        }
        else
        {
            PasswordBoxHidden.Password = PasswordBoxVisible.Text;
            PasswordBoxVisible.Visibility = Visibility.Collapsed;
            PasswordBoxHidden.Visibility = Visibility.Visible;
            PasswordBoxHidden.Focus();
        }
    }

    private void NumericPadToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_isNumericPadAnimating)
            return;

        if (NumericPadToggle.IsChecked == true)
            ShowNumericPad();
        else
            HideNumericPad();
    }

    private void NumPadDigit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        var digit = button.Tag?.ToString();
        if (string.IsNullOrEmpty(digit))
            return;

        AppendToPassword(digit);
    }

    private void NumPadBackspace_Click(object sender, RoutedEventArgs e)
    {
        var current = GetCurrentPassword();
        if (string.IsNullOrEmpty(current))
            return;

        SetPassword(current[..^1]);
    }

    private void NumPadClose_Click(object sender, RoutedEventArgs e)
    {
        CloseNumericPad(animate: true);
    }

    private void ShowNumericPad()
    {
        _isNumericPadAnimating = true;
        NumericPadPanel.Visibility = Visibility.Visible;
        NumericPadPanel.Opacity = 0;

        if (NumericPadPanel.RenderTransform is TranslateTransform transform)
            transform.Y = 16;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slideIn = new DoubleAnimation(16, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        fadeIn.Completed += (_, _) => _isNumericPadAnimating = false;

        NumericPadPanel.BeginAnimation(OpacityProperty, fadeIn);
        if (NumericPadPanel.RenderTransform is TranslateTransform slideTransform)
            slideTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);

        FocusActivePasswordField();
    }

    private void HideNumericPad()
    {
        if (NumericPadPanel.Visibility != Visibility.Visible)
        {
            _isNumericPadAnimating = false;
            return;
        }

        _isNumericPadAnimating = true;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var slideOut = new DoubleAnimation(0, 12, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (_, _) =>
        {
            NumericPadPanel.Visibility = Visibility.Collapsed;
            NumericPadPanel.BeginAnimation(OpacityProperty, null);
            if (NumericPadPanel.RenderTransform is TranslateTransform t)
                t.BeginAnimation(TranslateTransform.YProperty, null);
            _isNumericPadAnimating = false;
        };

        NumericPadPanel.BeginAnimation(OpacityProperty, fadeOut);
        if (NumericPadPanel.RenderTransform is TranslateTransform transform)
            transform.BeginAnimation(TranslateTransform.YProperty, slideOut);
    }

    private void CloseNumericPad(bool animate)
    {
        if (NumericPadToggle.IsChecked != true && NumericPadPanel.Visibility != Visibility.Visible)
            return;

        if (!animate)
        {
            _isNumericPadAnimating = true;
            NumericPadToggle.IsChecked = false;
            NumericPadPanel.BeginAnimation(OpacityProperty, null);
            if (NumericPadPanel.RenderTransform is TranslateTransform t)
            {
                t.BeginAnimation(TranslateTransform.YProperty, null);
                t.Y = 16;
            }

            NumericPadPanel.Opacity = 0;
            NumericPadPanel.Visibility = Visibility.Collapsed;
            _isNumericPadAnimating = false;
            return;
        }

        if (NumericPadToggle.IsChecked == true)
            NumericPadToggle.IsChecked = false;
        else
            HideNumericPad();
    }

    private string GetCurrentPassword()
    {
        return PasswordBoxVisible.Visibility == Visibility.Visible
            ? PasswordBoxVisible.Text
            : PasswordBoxHidden.Password;
    }

    private void AppendToPassword(string digit)
    {
        SetPassword(GetCurrentPassword() + digit);
    }

    private void SetPassword(string value)
    {
        if (PasswordBoxVisible.Visibility == Visibility.Visible)
        {
            PasswordBoxVisible.Text = value;
            PasswordBoxVisible.CaretIndex = PasswordBoxVisible.Text.Length;
        }
        else
        {
            PasswordBoxHidden.Password = value;
        }

        _viewModel.Password = value;
        FocusActivePasswordField();
    }

    private void FocusActivePasswordField()
    {
        if (PasswordBoxVisible.Visibility == Visibility.Visible)
            PasswordBoxVisible.Focus();
        else
            PasswordBoxHidden.Focus();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsSessionLockMode)
        {
            Application.Current.Shutdown();
            return;
        }

        DialogResult = false;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (IsSessionLockMode && DialogResult != true)
        {
            Application.Current.Shutdown();
        }

        base.OnClosing(e);
    }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        DragMove();
    }
}
