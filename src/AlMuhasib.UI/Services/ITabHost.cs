using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

/// <summary>A window that shows a strip of <see cref="DocumentTab"/> instances.</summary>
public interface ITabHost
{
    Window Window { get; }
    bool IsMainWindow { get; }
    ObservableCollection<DocumentTab> Tabs { get; }
    DocumentTab? SelectedTab { get; }
    ChromeTabStrip TabStrip { get; }
    FrameworkElement TemplateHost { get; }

    void Select(DocumentTab tab);
    bool TryClose(DocumentTab tab);
    void CloseOthers(DocumentTab keep);
    void CloseToTheVisualRight(DocumentTab tab);
    void CloseAll();
    void TogglePin(DocumentTab tab);
    void Reorder(DocumentTab tab, int newIndex);
    Task RefreshAsync(DocumentTab tab);

    /// <summary>Unparents the cached view and marks the tab so removal will not dispose it.</summary>
    void DetachVisual(DocumentTab tab);

    /// <summary>Removes the tab from this host without disposing its scope or view.</summary>
    void RemoveForTransfer(DocumentTab tab);

    /// <summary>Inserts a tab that already owns its view model and cached visual.</summary>
    void InsertTransferred(DocumentTab tab, int index);

    /// <summary>Closes a detached window after its last tab has been moved out.</summary>
    void CloseIfEmpty();
}
