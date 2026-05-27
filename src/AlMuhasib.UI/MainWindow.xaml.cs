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
        UpdateMaximizeIcon();
    }

    private void OnFirstLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnFirstLoaded;
        WindowState = WindowState.Maximized;

        if (Application.Current is App app)
        {
            var toast = app.Services.GetRequiredService<IToastNotificationService>();
            toast.AttachHost(AppToastHost);
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
