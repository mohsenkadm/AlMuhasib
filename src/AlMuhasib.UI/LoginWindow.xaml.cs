using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

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
        await _viewModel.LoadAdminsAsync();
    }

    private void OnStepChanged()
    {
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        DragMove();
    }
}
