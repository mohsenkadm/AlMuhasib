using System.Diagnostics;
using System.Windows;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.Windows;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Moves the existing <see cref="DocumentTab"/> (view model, scope, and visual) between windows.
/// A failed move puts the tab back on the source host.
/// </summary>
public sealed class TabWindowManager
{
    public static TabWindowManager Instance { get; } = new();

    private readonly List<ITabHost> _hosts = [];

    public void Register(ITabHost host)
    {
        if (!_hosts.Contains(host))
            _hosts.Add(host);
    }

    public void Unregister(ITabHost host) => _hosts.Remove(host);

    public IReadOnlyList<ITabHost> Hosts => _hosts;

    public IEnumerable<DocumentTab> DetachedTabs()
    {
        foreach (var host in _hosts)
        {
            if (host.IsMainWindow)
                continue;
            foreach (var tab in host.Tabs)
                yield return tab;
        }
    }

    public DocumentTab? Find(Func<DocumentTab, bool> predicate)
    {
        foreach (var host in _hosts)
        {
            var tab = host.Tabs.FirstOrDefault(predicate);
            if (tab is not null)
                return tab;
        }

        return null;
    }

    public bool TryActivate(DocumentTab tab)
    {
        var host = HostOf(tab);
        if (host is null)
            return false;

        host.Select(tab);
        var window = host.Window;
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
        return true;
    }

    public bool TryCloseForViewModel(ViewModelBase viewModel)
    {
        foreach (var host in _hosts.ToList())
        {
            var tab = host.Tabs.FirstOrDefault(t => ReferenceEquals(t.ViewModel, viewModel));
            if (tab is null)
                continue;
            return host.TryClose(tab);
        }

        return false;
    }

    public void Reorder(ITabHost host, DocumentTab tab, int newIndex) =>
        host.Reorder(tab, newIndex);

    public bool TryMove(DocumentTab tab, ITabHost from, ITabHost to, int index)
    {
        if (ReferenceEquals(from, to))
        {
            from.Reorder(tab, index);
            return true;
        }

        var originalIndex = from.Tabs.IndexOf(tab);
        if (originalIndex < 0)
            return false;

        try
        {
            from.DetachVisual(tab);
            tab.DetachVisualFromParent();
            if (tab.CachedView?.Parent is not null)
                throw new InvalidOperationException("الصفحة ما زالت مرتبطة بالنافذة الحالية.");

            from.RemoveForTransfer(tab);
            to.InsertTransferred(tab, index);

            if (!from.IsMainWindow && from.Tabs.Count == 0)
                from.CloseIfEmpty();

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Tabs] Transfer failed: {ex}");
            try
            {
                if (to.Tabs.Contains(tab))
                {
                    to.DetachVisual(tab);
                    tab.DetachVisualFromParent();
                    to.RemoveForTransfer(tab);
                }

                if (!from.Tabs.Contains(tab))
                    from.InsertTransferred(tab, Math.Min(originalIndex, from.Tabs.Count));
            }
            catch (Exception rollbackEx)
            {
                Debug.WriteLine($"[Tabs] Transfer rollback failed: {rollbackEx}");
            }

            tab.IsTransferring = false;
            BeautifulMessageDialog.ShowError("تعذّر نقل التبويب. بقيت الصفحة في نافذتها السابقة.");
            return false;
        }
    }

    public bool TryDetachToNewWindow(DocumentTab tab, ITabHost from, Point screenPixels)
    {
        if (from.Tabs.IndexOf(tab) < 0)
            return false;

        DetachedDocumentWindow window;
        try
        {
            window = new DetachedDocumentWindow();
            window.PlaceNear(screenPixels, from.Window);
            window.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Tabs] Detach window failed: {ex}");
            BeautifulMessageDialog.ShowError(
                $"تعذّر فتح نافذة مستقلة:\n\n{ex.InnerException?.Message ?? ex.Message}");
            return false;
        }

        var moved = TryMove(tab, from, window.Host, 0);
        if (!moved)
        {
            window.ForceCloseEmpty();
            return false;
        }

        window.Activate();
        return true;
    }

    public bool TryMoveToMain(DocumentTab tab)
    {
        var from = HostOf(tab);
        var main = _hosts.FirstOrDefault(h => h.IsMainWindow);
        if (from is null || main is null || ReferenceEquals(from, main))
            return false;

        return TryMove(tab, from, main, main.Tabs.Count);
    }

    public ITabHost? HostAt(Point screenPixels, double verticalSlop)
    {
        ITabHost? fallback = null;
        foreach (var host in _hosts)
        {
            if (!host.Window.IsVisible || host.TabStrip is null)
                continue;
            if (!host.TabStrip.ContainsScreenPoint(screenPixels, verticalSlop))
                continue;
            if (host.Window.IsActive)
                return host;
            fallback ??= host;
        }

        return fallback;
    }

    public void HideInsertionMarkers()
    {
        foreach (var host in _hosts)
            host.TabStrip?.HideInsertionMarker();
    }

    public void SetEnabled(bool enabled)
    {
        foreach (var host in _hosts)
        {
            if (!host.IsMainWindow)
                host.Window.IsEnabled = enabled;
        }
    }

    public bool TryCloseAllDetachedInteractive()
    {
        foreach (var host in _hosts.Where(h => !h.IsMainWindow).ToList())
        {
            var window = host.Window;
            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tabs] Detached close failed: {ex}");
                return false;
            }

            if (window.IsLoaded)
                return false;
        }

        return true;
    }

    public void ForceCloseAllDetached()
    {
        foreach (var window in _hosts.Where(h => !h.IsMainWindow).Select(h => h.Window).OfType<DetachedDocumentWindow>().ToList())
        {
            try
            {
                window.ForceClose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tabs] Force-close detached failed: {ex}");
            }
        }
    }

    private ITabHost? HostOf(DocumentTab tab) =>
        _hosts.FirstOrDefault(h => h.Tabs.Contains(tab));
}
