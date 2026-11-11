using System.Collections.Specialized;
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
    private readonly ISessionActivityService _sessionActivity;
    private readonly List<Guid> _tabViewLru = [];
    private DispatcherTimer? _idleTimer;
    private bool _isSessionLocked;
    private bool _isSyncingTabViews;
    private bool _tabViewSyncScheduled;

    public MainWindow(
        MainWindowViewModel viewModel,
        IUserPreferencesService preferences,
        ISessionActivityService sessionActivity)
    {
        InitializeComponent();
        Title = $"{AppInfo.AppNameAr} - {AppInfo.AppNameEn}";
        _viewModel = viewModel;
        _preferences = preferences;
        _sessionActivity = sessionActivity;
        DataContext = viewModel;
        WindowWorkAreaHelper.Enable(this);
        StateChanged += (_, _) => UpdateMaximizeIcon();
        Loaded += OnFirstLoaded;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.OpenTabs.CollectionChanged += OnOpenTabsChanged;
        _viewModel.SplitLayoutChanged += OnSplitLayoutChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewMouseDown += (_, _) => TouchActivity();
        PreviewMouseMove += (_, _) => TouchActivity();
        PreviewMouseWheel += (_, _) => TouchActivity();
        StartIdleLockTimer();
        UpdateMaximizeIcon();
    }

    private void TouchActivity() => _sessionActivity.RecordActivity();

    private void StartIdleLockTimer()
    {
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _idleTimer.Tick += (_, _) =>
        {
            var minutes = _preferences.Current.IdleLockMinutes;
            if (minutes <= 0 || _isSessionLocked) return;
            if ((DateTime.Now - _sessionActivity.LastActivity).TotalMinutes < minutes) return;
            PromptIdleReLogin();
        };
        _idleTimer.Start();
    }

    private void PromptIdleReLogin()
    {
        if (_isSessionLocked) return;

        // لا تعرض قفل الجلسة على نافذة أُغلقت أو لم تعد صالحة كمالك (Owner).
        if (!CanSetAsOwner(this))
            return;

        _isSessionLocked = true;
        _idleTimer?.Stop();

        var app = (App)Application.Current;
        var currentUser = app.Services.GetRequiredService<CurrentUserService>();
        currentUser.Clear();

        IsEnabled = false;

        try
        {
            while (true)
            {
                if (!CanSetAsOwner(this))
                {
                    Application.Current.Shutdown();
                    return;
                }

                var login = app.Services.GetRequiredService<LoginWindow>();
                login.IsSessionLockMode = true;
                TrySetOwner(login, this);

                var ok = login.ShowDialog() == true;
                if (!ok)
                {
                    Application.Current.Shutdown();
                    return;
                }

                var mainVm = app.Services.GetRequiredService<MainWindowViewModel>();
                mainVm.LoggedInUsername = currentUser.Username;
                mainVm.RefreshLoggedInUserRoleLabel();
                mainVm.RefreshCurrentBranchDisplay();
                _ = mainVm.ApplyPermissionsAsync();
                TouchActivity();
                break;
            }
        }
        finally
        {
            if (CanSetAsOwner(this))
                IsEnabled = true;
            _isSessionLocked = false;
            if (CanSetAsOwner(this))
                _idleTimer?.Start();
        }
    }

    private static bool CanSetAsOwner(Window? window)
    {
        if (window is null) return false;
        try
        {
            // نافذة مغلقة تفشل عند تعيينها Owner وتُطلق InvalidOperationException.
            return window.IsLoaded
                   && PresentationSource.FromVisual(window) is not null;
        }
        catch
        {
            return false;
        }
    }

    private static void TrySetOwner(Window dialog, Window? owner)
    {
        if (!CanSetAsOwner(owner))
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        try
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        catch (InvalidOperationException)
        {
            dialog.Owner = null;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
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

        if (e.Key == Key.T && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            if (_viewModel.ReopenClosedTabCommand.CanExecute(null))
                _ = _viewModel.ReopenClosedTabCommand.ExecuteAsync(null);
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

        if (e.Key == Key.Escape && _viewModel.IsOpenRecentExcelPanelOpen)
        {
            _viewModel.CloseOpenRecentExcelPanelCommand.Execute(null);
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

        if (e.Key == Key.Escape && _viewModel.ShowKeyboardShortcutsHelp)
        {
            _viewModel.ShowKeyboardShortcutsHelp = false;
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

        ScheduleSyncTabViews();
        SizeChanged += (_, _) => UpdateFabScale();
        UpdateFabScale();
    }

    private void UpdateFabScale()
    {
        if (ActualWidth <= 1 || ActualHeight <= 1)
            return;

        var byHeight = ActualHeight / 980.0;
        var byWidth = ActualWidth / 1366.0;
        var visual = Math.Clamp(Math.Min(byHeight, byWidth), 0.56, 1.05);
        var zoom = Math.Max(0.5, _viewModel.MainContentZoom);
        var scale = visual / zoom;
        FabScaleTransform.ScaleX = scale;
        FabScaleTransform.ScaleY = scale;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.MainContentZoom))
            UpdateFabScale();

        if (e.PropertyName is nameof(MainWindowViewModel.SelectedTab)
            or nameof(MainWindowViewModel.SecondaryTab)
            or nameof(MainWindowViewModel.CurrentViewModel)
            or nameof(MainWindowViewModel.SecondaryViewModel)
            or nameof(MainWindowViewModel.IsSplitViewActive))
        {
            ScheduleSyncTabViews();
        }
    }

    private void OnOpenTabsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (DocumentTab tab in e.OldItems)
            {
                tab.ReleaseCachedView();
                _tabViewLru.Remove(tab.Id);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _tabViewLru.Clear();
            if (PrimaryTabContentHost is not null)
                PrimaryTabContentHost.Content = null;
            if (SecondaryTabContentHost is not null)
                SecondaryTabContentHost.Content = null;
        }

        ScheduleSyncTabViews();
    }

    private void ScheduleSyncTabViews()
    {
        if (_tabViewSyncScheduled)
            return;

        _tabViewSyncScheduled = true;
        Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () =>
        {
            _tabViewSyncScheduled = false;
            SyncTabViews();
        });
    }

    private void SyncTabViews()
    {
        if (_isSyncingTabViews || PrimaryTabContentHost is null || SplitContentGrid is null)
            return;

        _isSyncingTabViews = true;
        try
        {
            var primary = _viewModel.SelectedTab;
            var secondary = _viewModel.IsSplitViewActive ? _viewModel.SecondaryTab : null;
            var primaryChanged = false;

            if (primary is not null)
            {
                TabViewCache.TouchLru(_tabViewLru, primary.Id);
                var view = TabViewCache.EnsureView(primary, SplitContentGrid);
                if (!ReferenceEquals(PrimaryTabContentHost.Content, view))
                {
                    PrimaryTabContentHost.Content = view;
                    primaryChanged = true;
                }
            }
            else if (PrimaryTabContentHost.Content is not null)
            {
                PrimaryTabContentHost.Content = null;
                primaryChanged = true;
            }

            if (SecondaryTabContentHost is not null)
            {
                if (secondary is not null)
                {
                    TabViewCache.TouchLru(_tabViewLru, secondary.Id);
                    var view = TabViewCache.EnsureView(secondary, SplitContentGrid);
                    if (!ReferenceEquals(SecondaryTabContentHost.Content, view))
                        SecondaryTabContentHost.Content = view;
                }
                else if (SecondaryTabContentHost.Content is not null)
                {
                    SecondaryTabContentHost.Content = null;
                }
            }

            TabViewCache.Trim(_viewModel.OpenTabs, primary, secondary, _tabViewLru);

            if (primaryChanged)
                PlayTabContentAnimation();
        }
        finally
        {
            _isSyncingTabViews = false;
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

    private void TelegramSubscribeBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.CloseTelegramSubscribeDialogCommand.Execute(null);

    private void MobileSubscribeBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.CloseMobileSubscribeDialogCommand.Execute(null);

    private void MobileDownloadBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.CloseMobileDownloadDialogCommand.Execute(null);

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

    private void OpenRecentExcelBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.CloseOpenRecentExcelPanelCommand.Execute(null);

    private void KeyboardShortcutsBackdrop_Click(object sender, MouseButtonEventArgs e) =>
        _viewModel.ShowKeyboardShortcutsHelp = false;

    private void KeyboardShortcutsCard_Click(object sender, MouseButtonEventArgs e) =>
        e.Handled = true;

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

    private void ChromeTabContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu { PlacementTarget: FrameworkElement target })
            return;

        var tab = target.DataContext as DocumentTab;
        _viewModel.PrepareTabContextMenu(tab);
    }

    /// <summary>
    /// GridSplitter writes local Width values that break bindings; always reset via ClearValue.
    /// </summary>
    private void OnSplitLayoutChanged(bool split)
    {
        if (SplitPrimaryColumn is null || SplitSplitterColumn is null || SplitSecondaryColumn is null)
            return;

        SplitPrimaryColumn.ClearValue(ColumnDefinition.WidthProperty);
        SplitSplitterColumn.ClearValue(ColumnDefinition.WidthProperty);
        SplitSecondaryColumn.ClearValue(ColumnDefinition.WidthProperty);

        if (split)
        {
            SplitPrimaryColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitSplitterColumn.Width = new GridLength(6);
            SplitSecondaryColumn.Width = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            SplitPrimaryColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitSplitterColumn.Width = new GridLength(0);
            SplitSecondaryColumn.Width = new GridLength(0);
        }
    }

    private void TitleBarSearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.SetTitleBarSearchFocused(true);
    }

    private void TitleBarSearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // Keep popup open while interacting with its results.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            if (TitleBarSearchBox?.IsKeyboardFocusWithin == true)
                return;
            if (TitleBarSearchPopup?.IsMouseOver == true)
                return;
            _viewModel.SetTitleBarSearchFocused(false);
        });
    }

    private void TitleBarSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _viewModel.ClearTitleBarSearchCommand.Execute(null);
            _viewModel.SetTitleBarSearchFocused(false);
            Keyboard.ClearFocus();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && _viewModel.SearchResults.Count == 1)
        {
            _viewModel.SelectSearchResultCommand.Execute(_viewModel.SearchResults[0]);
            e.Handled = true;
        }
    }

    private void TitleBarSearchPopup_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Keep focus semantics with the search field while using the popup.
        e.Handled = false;
    }

    private void TitleBarSearchResult_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button button)
            return;

        var parameter = button.CommandParameter;
        if (button.Command?.CanExecute(parameter) == true)
        {
            button.Command.Execute(parameter);
            e.Handled = true;
        }
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);

        if (_viewModel.IsTitleBarSearchPopupOpen
            && TitleBarSearchHost?.IsMouseOver != true
            && TitleBarSearchPopup?.IsMouseOver != true)
        {
            _viewModel.SetTitleBarSearchFocused(false);
        }

        // Close header options when clicking outside the gear and popup.
        if (_viewModel.IsHeaderOptionsOpen
            && HeaderOptionsButton?.IsMouseOver != true
            && HeaderOptionsPopup?.IsMouseOver != true)
        {
            _viewModel.IsHeaderOptionsOpen = false;
        }
    }
}
