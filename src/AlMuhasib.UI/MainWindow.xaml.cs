using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        WindowWorkAreaHelper.Enable(this);
        StateChanged += (_, _) => UpdateMaximizeIcon();
        Loaded += OnFirstLoaded;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        UpdateMaximizeIcon();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.OpenGlobalSearchCommand.Execute(null);
            if (GlobalSearchOverlay.Visibility == Visibility.Visible)
                GlobalSearchOverlay.FocusSearch();
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
