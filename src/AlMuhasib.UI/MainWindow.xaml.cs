using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.Windows;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly IUserPreferencesService _preferences;
    private DispatcherTimer? _idleTimer;
    private DateTime _lastActivity = DateTime.Now;
    private bool _isSessionLocked;

    public MainWindow(MainWindowViewModel viewModel, IUserPreferencesService preferences)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _preferences = preferences;
        DataContext = viewModel;
        WindowWorkAreaHelper.Enable(this);
        StateChanged += (_, _) => UpdateMaximizeIcon();
        Loaded += OnFirstLoaded;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewMouseDown += (_, _) => TouchActivity();
        PreviewMouseMove += (_, _) => TouchActivity();
        PreviewMouseWheel += (_, _) => TouchActivity();
        StartIdleLockTimer();
        UpdateMaximizeIcon();
    }

    private void TouchActivity() => _lastActivity = DateTime.Now;

    private void StartIdleLockTimer()
    {
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _idleTimer.Tick += (_, _) =>
        {
            var minutes = _preferences.Current.IdleLockMinutes;
            if (minutes <= 0 || _isSessionLocked) return;
            if ((DateTime.Now - _lastActivity).TotalMinutes < minutes) return;
            PromptIdleReLogin();
        };
        _idleTimer.Start();
    }

    private void PromptIdleReLogin()
    {
        if (_isSessionLocked) return;
        _isSessionLocked = true;
        _idleTimer?.Stop();

        var app = (App)Application.Current;
        var currentUser = app.Services.GetRequiredService<CurrentUserService>();
        currentUser.Clear();

        IsEnabled = false;

        while (true)
        {
            var login = app.Services.GetRequiredService<LoginWindow>();
            login.IsSessionLockMode = true;
            login.Owner = this;
            login.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var ok = login.ShowDialog() == true;
            if (!ok)
            {
                Application.Current.Shutdown();
                return;
            }

            var mainVm = app.Services.GetRequiredService<MainWindowViewModel>();
            mainVm.LoggedInUsername = currentUser.Username;
            _ = mainVm.ApplyPermissionsAsync();
            TouchActivity();
            break;
        }

        IsEnabled = true;
        _isSessionLocked = false;
        _idleTimer?.Start();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        TouchActivity();

        if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _ = _viewModel.ToggleVoiceAssistantCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsVoiceAssistantOpen)
        {
            _viewModel.CloseVoiceAssistantCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.OpenGlobalSearchCommand.Execute(null);
            if (GlobalSearchOverlay.Visibility == Visibility.Visible)
                GlobalSearchOverlay.FocusSearch();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsTasksPanelOpen)
        {
            _viewModel.CloseTasksPanelCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsNotesPanelOpen)
        {
            _ = _viewModel.CloseNotesPanelCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsNotificationPanelOpen)
        {
            _viewModel.CloseNotificationPanelCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsGlobalSearchOpen)
        {
            _viewModel.CloseGlobalSearchCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F1)
        {
            _viewModel.ToggleKeyboardShortcutsHelpCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F2 && Keyboard.Modifiers == ModifierKeys.None)
        {
            _ = _viewModel.QuickNewSaleCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F3 && Keyboard.Modifiers == ModifierKeys.None)
        {
            _viewModel.OpenGlobalSearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnFirstLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnFirstLoaded;
        WindowState = WindowState.Maximized;

        if (Application.Current is App app)
        {
            var toast = app.Services.GetRequiredService<IToastNotificationService>();
            toast.AttachHost(AppToastHost);

            var theme = app.Services.GetRequiredService<ThemeService>();
            theme.ApplyFromPreferences();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.TabContentGeneration)
            or nameof(MainWindowViewModel.CurrentViewModel))
        {
            PlayTabContentAnimation();
        }
    }

    private void PlayTabContentAnimation()
    {
        if (TabContentHost is null)
            return;

        if (FindResource("TabContentFadeIn") is not Storyboard storyboard)
            return;

        var clone = storyboard.Clone();
        clone.Begin(TabContentHost);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_viewModel.IsExitConfirmed)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        _viewModel.IsExitDialogOpen = true;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        ToggleMaximize();

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void UpdateMaximizeIcon()
    {
        if (MaximizeIcon is null)
            return;

        MaximizeIcon.Kind = WindowState == WindowState.Maximized
            ? PackIconKind.WindowRestore
            : PackIconKind.WindowMaximize;
    }

    private void QuickAssistBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.IsQuickAssistOpen = false;

    private void SmartAssistantBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.IsSmartAssistantOpen = false;

    private void SmartAssistantClose_Click(object sender, RoutedEventArgs e) =>
        _viewModel.IsSmartAssistantOpen = false;

    private void NotificationBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.IsNotificationPanelOpen = false;

    private void TasksBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.IsTasksPanelOpen = false;

    private void NotesBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _ = _viewModel.CloseNotesPanelCommand.ExecuteAsync(null);

    private void ChromeTabClose_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button { Command: { } command, CommandParameter: { } parameter }
            && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }

        // منع اختيار التبويب عند الضغط على زر الإغلاق فقط
        e.Handled = true;
    }
}
