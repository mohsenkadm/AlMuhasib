using System.Windows;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Builds per-tab view cache so tab switches reuse visual trees for every open tab
/// (up to <see cref="ViewModels.MainWindowViewModel.MaxOpenTabs"/>).
/// </summary>
public static class TabViewCache
{
    public static FrameworkElement? EnsureView(DocumentTab tab, FrameworkElement resourceHost)
    {
        if (tab.CachedView is not null)
            return tab.CachedView;

        var key = new DataTemplateKey(tab.ViewModel.GetType());
        if (resourceHost.TryFindResource(key) is not DataTemplate template)
            return null;

        if (template.LoadContent() is not FrameworkElement view)
            return null;

        view.DataContext = tab.ViewModel;
        tab.CachedView = view;
        return view;
    }

    public static void TouchLru(IList<Guid> lruOrder, Guid tabId)
    {
        for (var i = lruOrder.Count - 1; i >= 0; i--)
        {
            if (lruOrder[i] == tabId)
                lruOrder.RemoveAt(i);
        }

        lruOrder.Insert(0, tabId);
    }

    /// <summary>
    /// Keeps cached views for all still-open tabs; only drops LRU entries for closed tabs.
    /// </summary>
    public static void Trim(
        IEnumerable<DocumentTab> openTabs,
        DocumentTab? primary,
        DocumentTab? secondary,
        IList<Guid> lruOrder)
    {
        _ = primary;
        _ = secondary;

        var openIds = new HashSet<Guid>();
        foreach (var tab in openTabs)
            openIds.Add(tab.Id);

        for (var i = lruOrder.Count - 1; i >= 0; i--)
        {
            if (!openIds.Contains(lruOrder[i]))
                lruOrder.RemoveAt(i);
        }
    }
}
