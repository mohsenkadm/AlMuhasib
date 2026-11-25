using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AlMuhasib.UI;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Windows;

public partial class DetachedDocumentWindow : Window
{
    private readonly MainWindowViewModel _main;
    private readonly ISessionActivityService _activity;
    private readonly DetachedTabsState _state = new();
    private readonly DetachedTabHost _host;
    private bool _forceClose;
    private bool _emptyClose;
    private bool _syncing;
    private Guid _shownTabId;

    public DetachedDocumentWindow()
    {
        var app = (App)Application.Current;
        _main = app.Services.GetRequiredService<MainWindowViewModel>();
        _activity = app.Services.GetRequiredService<ISessionActivityService>();
        InitializeComponent();
        DataContext = _state;
        _host = new DetachedTabHost(this);
        ChromeTabs.AttachHost(_host);
        TabWindowManager.Instance.Register(_host);
        _state.PropertyChanged += OnStatePropertyChanged;
        _state.Tabs.CollectionChanged += (_, _) => SyncContent(animate: false);
        Closed += (_, _) => TabWindowManager.Instance.Unregister(_host);
        Closing += OnClosing;
        StateChanged += (_, _) => UpdateMaximizeIcon();
        PreviewMouseDown += (_, _) => _activity.RecordActivity();
        PreviewKeyDown += (_, _) => _activity.RecordActivity();
        WindowWorkAreaHelper.Enable(this);
        UpdateMaximizeIcon();
    }

    public ITabHost Host => _host;

    public void PlaceNear(Point screenPixels, Visual relativeTo)
    {
        var dip = ToDip(relativeTo, screenPixels);
        Left = dip.X - 80;
        Top = dip.Y - 52;
    }

    public void ForceClose()
    {
        _forceClose = true;
        _syncing = true;
        DocumentHost.Content = null;
        foreach (var tab in _state.Tabs.ToList())
        {
            _state.Tabs.Remove(tab);
            try
            {
                tab.Dispose();
            }
            catch
            {
                // Shutdown must not stick on a single scope.
            }
        }

        Close();
    }

    public void ForceCloseEmpty()
    {
        _emptyClose = true;
        _forceClose = true;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_forceClose || _emptyClose)
            return;

        foreach (var tab in _state.Tabs.ToList())
        {
            if (_host.TryClose(tab, closeWindowWhenEmpty: false))
                continue;

            e.Cancel = true;
            return;
        }
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DetachedTabsState.SelectedTab))
            SyncContent(animate: true);
    }

    private void SyncContent(bool animate)
    {
        if (_syncing || DocumentHost is null)
            return;

        _syncing = true;
        try
        {
            var tab = _state.SelectedTab;
            if (tab is null || tab.IsTransferring)
            {
                if (tab is null)
                {
                    DocumentHost.Content = null;
                    Title = AppInfo.AppNameAr;
                    _shownTabId = Guid.Empty;
                }

                return;
            }

            Title = $"{tab.Title} — {AppInfo.AppNameAr}";
            var templateHost = TemplateHost();
            var view = TabViewCache.EnsureView(tab, templateHost);
            if (!ReferenceEquals(DocumentHost.Content, view))
            {
                DocumentHost.Content = view;
                if (animate && tab.Id != _shownTabId)
                    BeginAnimation();
            }

            _shownTabId = tab.Id;
        }
        finally
        {
            _syncing = false;
        }
    }

    private FrameworkElement TemplateHost()
    {
        var main = Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault();
        return main?.TabTemplateHost ?? DocumentSurface;
    }

    private void BeginAnimation()
    {
        DocumentSurface.Opacity = 0.92;
        var animation = new System.Windows.Media.Animation.DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(90))
        {
            EasingFunction = new System.Windows.Media.Animation.CubicEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            }
        };
        DocumentSurface.BeginAnimation(OpacityProperty, animation);
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

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

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

    private static Point ToDip(Visual relativeTo, Point screenPixels)
    {
        var source = PresentationSource.FromVisual(relativeTo);
        if (source?.CompositionTarget is null)
            return screenPixels;
        return source.CompositionTarget.TransformFromDevice.Transform(screenPixels);
    }

    private sealed partial class DetachedTabsState : ObservableObject
    {
        public ObservableCollection<DocumentTab> Tabs { get; } = [];

        [ObservableProperty]
        private DocumentTab? _selectedTab;
    }

    private sealed class DetachedTabHost : ITabHost
    {
        private readonly DetachedDocumentWindow _window;

        public DetachedTabHost(DetachedDocumentWindow window) => _window = window;

        public Window Window => _window;
        public bool IsMainWindow => false;
        public ObservableCollection<DocumentTab> Tabs => _window._state.Tabs;
        public DocumentTab? SelectedTab => _window._state.SelectedTab;
        public ChromeTabStrip TabStrip => _window.ChromeTabs;
        public FrameworkElement TemplateHost => _window.TemplateHost();

        public void Select(DocumentTab tab)
        {
            if (Tabs.Contains(tab))
                _window._state.SelectedTab = tab;
        }

        public bool TryClose(DocumentTab tab) => TryClose(tab, closeWindowWhenEmpty: true);

        public bool TryClose(DocumentTab tab, bool closeWindowWhenEmpty)
        {
            if (!Tabs.Contains(tab) || !_window._main.ConfirmTabClose(tab))
                return false;

            var index = Tabs.IndexOf(tab);
            var wasSelected = ReferenceEquals(_window._state.SelectedTab, tab);
            Tabs.Remove(tab);
            _window._main.DisposeDetachedTab(tab);

            if (wasSelected && Tabs.Count > 0)
                _window._state.SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
            else
                _window.SyncContent(animate: false);

            if (closeWindowWhenEmpty && Tabs.Count == 0 && !_window._emptyClose)
            {
                _window._emptyClose = true;
                _window.Close();
            }

            return true;
        }

        public void CloseOthers(DocumentTab keep)
        {
            Select(keep);
            foreach (var other in Tabs.Where(t => !ReferenceEquals(t, keep) && t.CanClose).ToList())
            {
                if (!TryClose(other))
                    return;
            }
        }

        public void CloseToTheVisualRight(DocumentTab tab)
        {
            var index = Tabs.IndexOf(tab);
            if (index < 0)
                return;

            foreach (var other in Tabs.Where((_, i) => i < index).Where(t => t.CanClose).ToList())
            {
                if (!TryClose(other))
                    return;
            }
        }

        public void CloseAll()
        {
            foreach (var tab in Tabs.Where(t => t.CanClose).ToList())
            {
                if (!TryClose(tab))
                    return;
            }
        }

        public void TogglePin(DocumentTab tab) => _window._main.TogglePinFromHost(tab);

        public void Reorder(DocumentTab tab, int newIndex)
        {
            var oldIndex = Tabs.IndexOf(tab);
            if (oldIndex < 0)
                return;

            newIndex = Math.Clamp(newIndex, 0, Tabs.Count - 1);
            if (oldIndex == newIndex)
                return;

            Tabs.Move(oldIndex, newIndex);
        }

        public Task RefreshAsync(DocumentTab tab) => TabRefresh.RunAsync(tab);

        public void DetachVisual(DocumentTab tab)
        {
            tab.IsTransferring = true;
            if (ReferenceEquals(_window.DocumentHost.Content, tab.CachedView))
                _window.DocumentHost.Content = null;
            tab.DetachVisualFromParent();
        }

        public void RemoveForTransfer(DocumentTab tab)
        {
            tab.IsTransferring = true;
            var index = Tabs.IndexOf(tab);
            if (index < 0)
                return;

            var wasSelected = ReferenceEquals(_window._state.SelectedTab, tab);
            Tabs.Remove(tab);
            if (wasSelected && Tabs.Count > 0)
                _window._state.SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
        }

        public void InsertTransferred(DocumentTab tab, int index)
        {
            if (!Tabs.Contains(tab))
            {
                tab.CanClose = true;
                Tabs.Insert(Math.Clamp(index, 0, Tabs.Count), tab);
            }
            else
            {
                Reorder(tab, index);
            }

            tab.IsTransferring = false;
            _window._state.SelectedTab = tab;
            _window.SyncContent(animate: false);
            _window._main.UpdatePinStatesFromHost();
        }

        public void CloseIfEmpty()
        {
            if (Tabs.Count > 0 || _window._emptyClose)
                return;

            _window._emptyClose = true;
            _window.Close();
        }
    }
}
