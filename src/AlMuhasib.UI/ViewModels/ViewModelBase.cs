using AlMuhasib.Core.Interfaces;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    // ── Column filters ─────────────────────────────────────
    [ObservableProperty]
    private bool _isColumnFilterPanelOpen;

    [ObservableProperty]
    private int _activeColumnFilterCount;

    protected Dictionary<string, string> ColumnFilters { get; } = new(StringComparer.OrdinalIgnoreCase);

    [RelayCommand]
    private void ApplyColumnFilters(Dictionary<string, string>? filters)
    {
        ColumnFilters.Clear();
        if (filters is not null)
        {
            foreach (var kv in filters)
                ColumnFilters[kv.Key] = kv.Value;
        }

        ActiveColumnFilterCount = ColumnFilters.Count(kv => !string.IsNullOrWhiteSpace(kv.Value));
        OnColumnFiltersChanged();
    }

    [RelayCommand]
    private void ClearColumnFilters()
    {
        ColumnFilters.Clear();
        ActiveColumnFilterCount = 0;
        OnColumnFiltersChanged();
    }

    protected virtual void OnColumnFiltersChanged() { }

    // ── Permissions (default false until LoadPermissions) ──
    [ObservableProperty]
    private bool _canAdd;

    [ObservableProperty]
    private bool _canEdit;

    [ObservableProperty]
    private bool _canDelete;

    [ObservableProperty]
    private bool _canPrint;

    [ObservableProperty]
    private bool _canExport;

    protected void LoadPermissions(ICurrentUserService currentUserService, string screenName)
    {
        if (currentUserService.IsAdmin)
        {
            CanAdd = true;
            CanEdit = true;
            CanDelete = true;
            CanPrint = true;
            CanExport = true;
            return;
        }

        CanAdd = currentUserService.CanAdd(screenName) && !currentUserService.IsViewOnly(screenName);
        CanEdit = currentUserService.CanEdit(screenName) && !currentUserService.IsViewOnly(screenName);
        CanDelete = currentUserService.CanDelete(screenName) && !currentUserService.IsViewOnly(screenName);
        CanPrint = currentUserService.CanPrint(screenName);
        CanExport = currentUserService.CanExport(screenName);
    }

    protected static bool RequestSensitiveApproval(string message) =>
        BeautifulMessageDialog.ShowConfirm(message, "موافقة على عملية حساسة");

    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Override to indicate there are unsaved changes that should prompt the user before navigating away.
    /// </summary>
    public virtual bool HasUnsavedChanges => false;

    /// <summary>
    /// When true, the leave prompt offers Save / Discard / Cancel and calls <see cref="SavePendingChangesAsync"/>.
    /// </summary>
    public virtual bool SupportsSaveBeforeLeave => false;

    /// <summary>
    /// Called when the user chooses «حفظ» before leaving a dirty screen.
    /// Return true if save succeeded (or there was nothing to save); false to cancel navigation.
    /// </summary>
    public virtual Task<bool> SavePendingChangesAsync() => Task.FromResult(true);
}
