using System.Windows;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Builds per-tab view cache so tab switches reuse visual trees for every open tab
/// (up to <see cref="ViewModels.MainWindowViewModel.MaxOpenTabs"/>).
/// </summary>
public static class TabViewCache
{
    /// <summary>
    /// Templates live on the main window content grid. Detached windows use this lookup
    /// so a tab that was never shown can still be built once and then moved as the same visual.
    /// </summary>
    public static Func<Type, DataTemplate?>? ExternalTemplateLookup { get; set; }

    public static FrameworkElement? EnsureView(DocumentTab tab, FrameworkElement resourceHost)
    {
        if (tab.CachedView is not null)
            return tab.CachedView;

        var template = FindTemplate(resourceHost, tab.ViewModel.GetType());
        if (template is null)
            return null;

        if (template.LoadContent() is not FrameworkElement view)
            return null;

        view.DataContext = tab.ViewModel;
        tab.CachedView = view;
        return view;
    }

    private static DataTemplate? FindTemplate(FrameworkElement resourceHost, Type viewModelType)
    {
        var key = new DataTemplateKey(viewModelType);
        if (resourceHost.TryFindResource(key) is DataTemplate local)
            return local;

        return ExternalTemplateLookup?.Invoke(viewModelType);
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
