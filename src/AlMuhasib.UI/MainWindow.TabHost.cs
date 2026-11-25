using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI;

public partial class MainWindow
{
    private MainTabHost? _chromeHost;

    internal FrameworkElement TabTemplateHost => SplitContentGrid;

    private void InitializeChromeTabHost()
    {
        TabViewCacheHook();
        _chromeHost = new MainTabHost(this, _viewModel);
        ChromeTabs.AttachHost(_chromeHost);
        TabWindowManager.Instance.Register(_chromeHost);
        Closed += (_, _) =>
        {
            if (_chromeHost is not null)
                TabWindowManager.Instance.Unregister(_chromeHost);
        };
    }

    private void TabViewCacheHook()
    {
        Helpers.TabViewCache.ExternalTemplateLookup = type =>
        {
            if (SplitContentGrid is null)
                return null;
            return SplitContentGrid.TryFindResource(new DataTemplateKey(type)) as DataTemplate;
        };
    }

    internal void DetachTabVisual(DocumentTab tab)
    {
        tab.IsTransferring = true;
        if (ReferenceEquals(PrimaryTabContentHost.Content, tab.CachedView))
            PrimaryTabContentHost.Content = null;
        if (SecondaryTabContentHost is not null && ReferenceEquals(SecondaryTabContentHost.Content, tab.CachedView))
            SecondaryTabContentHost.Content = null;
        tab.DetachVisualFromParent();
    }

    private sealed class MainTabHost : ITabHost
    {
        private readonly MainWindow _window;
        private readonly MainWindowViewModel _viewModel;

        public MainTabHost(MainWindow window, MainWindowViewModel viewModel)
        {
            _window = window;
            _viewModel = viewModel;
        }

        public Window Window => _window;
        public bool IsMainWindow => true;
        public ObservableCollection<DocumentTab> Tabs => _viewModel.OpenTabs;
        public DocumentTab? SelectedTab => _viewModel.SelectedTab;
        public ChromeTabStrip TabStrip => _window.ChromeTabs;
        public FrameworkElement TemplateHost => _window.SplitContentGrid;

        public void Select(DocumentTab tab) => _viewModel.ActivateSessionTab(tab);

        public bool TryClose(DocumentTab tab)
        {
            if (!_viewModel.OpenTabs.Contains(tab))
                return false;

            var count = _viewModel.OpenTabs.Count;
            _viewModel.CloseTabCommand.Execute(tab);
            return !_viewModel.OpenTabs.Contains(tab) || _viewModel.OpenTabs.Count < count;
        }

        public void CloseOthers(DocumentTab keep) => _viewModel.CloseOtherTabsCommand.Execute(keep);

        public void CloseToTheVisualRight(DocumentTab tab) => _viewModel.CloseTabsToTheRightCommand.Execute(tab);

        public void CloseAll() => _viewModel.CloseAllClosableTabsCommand.Execute(null);

        public void TogglePin(DocumentTab tab) => _viewModel.TogglePinFromHost(tab);

        public void Reorder(DocumentTab tab, int newIndex) => _viewModel.ReorderOpenTab(tab, newIndex);

        public Task RefreshAsync(DocumentTab tab) => TabRefresh.RunAsync(tab);

        public void DetachVisual(DocumentTab tab) => _window.DetachTabVisual(tab);

        public void RemoveForTransfer(DocumentTab tab) => _viewModel.RemoveOpenTabForTransfer(tab);

        public void InsertTransferred(DocumentTab tab, int index) => _viewModel.AcceptTransferredOpenTab(tab, index);

        public void CloseIfEmpty()
        {
            // The main window stays open and restores the dashboard when its strip is empty.
        }
    }
}
