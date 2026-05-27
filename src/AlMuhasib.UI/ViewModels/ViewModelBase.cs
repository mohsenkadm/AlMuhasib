using AlMuhasib.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    // ── Permissions (default true; override via LoadPermissions) ──
    [ObservableProperty]
    private bool _canAdd = true;

    [ObservableProperty]
    private bool _canEdit = true;

    [ObservableProperty]
    private bool _canDelete = true;

    [ObservableProperty]
    private bool _canPrint = true;

    [ObservableProperty]
    private bool _canExport = true;

    protected void LoadPermissions(ICurrentUserService currentUserService, string screenName)
    {
        CanAdd = currentUserService.CanAdd(screenName);
        CanEdit = currentUserService.CanEdit(screenName);
        CanDelete = currentUserService.CanDelete(screenName);
        CanPrint = currentUserService.CanPrint(screenName);
        CanExport = currentUserService.CanExport(screenName);
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Override to indicate there are unsaved changes that should prompt the user before navigating away.
    /// </summary>
    public virtual bool HasUnsavedChanges => false;
}
