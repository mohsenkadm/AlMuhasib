using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class PermissionsViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    public PermissionsViewModel(IAuthService authService)
    {
        _authService = authService;
        PageTitle = "الصلاحيات";
        InitializeScreens();
    }

    public ObservableCollection<UserRow> Users { get; } = [];

    [ObservableProperty]
    private UserRow? _selectedUser;

    partial void OnSelectedUserChanged(UserRow? value)
    {
        if (value is not null)
            _ = LoadPermissionsAsync();
    }

    public ObservableCollection<ScreenPermissionRow> Screens { get; } = [];

    private void InitializeScreens()
    {
        Screens.Clear();
        foreach (var (name, label) in ScreenPermissionRegistry.AllScreens)
            Screens.Add(new ScreenPermissionRow { ScreenName = name, ScreenLabel = label });
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;
            var users = await _authService.GetAllUsersAsync();
            Users.Clear();
            foreach (var u in users)
            {
                Users.Add(new UserRow
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role,
                    RoleDisplay = u.Role == Core.Enums.UserRole.Admin ? "مدير" : "مستخدم",
                    IsActive = u.IsActive,
                    StatusDisplay = u.IsActive ? "فعال" : "معطّل"
                });
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadPermissionsAsync()
    {
        if (SelectedUser is null) return;
        try
        {
            IsBusy = true;
            var permissions = await _authService.GetUserPermissionsAsync(SelectedUser.Id);

            foreach (var s in Screens)
            {
                s.CanView = s.ScreenName == ScreenPermissionRegistry.Dashboard;
                s.CanAdd = false;
                s.CanEdit = false;
                s.CanDelete = false;
                s.CanPrint = false;
                s.CanExport = false;
                s.CanEditPrice = false;
                s.IsViewOnly = false;
            }

            foreach (var p in permissions)
            {
                var screen = Screens.FirstOrDefault(s => s.ScreenName == p.ScreenName);
                if (screen is null) continue;
                screen.CanView = p.ScreenName == ScreenPermissionRegistry.Dashboard || p.CanView;
                screen.CanAdd = p.CanAdd;
                screen.CanEdit = p.CanEdit;
                screen.CanDelete = p.CanDelete;
                screen.CanPrint = p.CanPrint;
                screen.CanExport = p.CanExport;
                screen.CanEditPrice = p.CanEditPrice;
                screen.IsViewOnly = p.IsViewOnly;
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SavePermissionsAsync()
    {
        if (SelectedUser is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار مستخدم أولاً");
            return;
        }

        try
        {
            IsBusy = true;
            var permissions = Screens.Select(s => new Permission
            {
                ScreenName = s.ScreenName,
                CanView = s.ScreenName == ScreenPermissionRegistry.Dashboard || s.CanView,
                CanAdd = s.CanView && s.CanAdd,
                CanEdit = s.CanView && s.CanEdit,
                CanDelete = s.CanView && s.CanDelete,
                CanPrint = s.CanView && s.CanPrint,
                CanExport = s.CanView && s.CanExport,
                CanEditPrice = s.CanView && s.CanEditPrice,
                IsViewOnly = s.CanView && s.IsViewOnly
            }).ToList();

            await _authService.SaveUserPermissionsAsync(SelectedUser.Id, permissions);
            BeautifulMessageDialog.ShowSuccess("تم حفظ الصلاحيات بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var s in Screens)
        {
            s.CanView = true;
            s.CanAdd = true;
            s.CanEdit = true;
            s.CanDelete = true;
            s.CanPrint = true;
            s.CanExport = true;
            s.CanEditPrice = true;
            s.IsViewOnly = false;
        }
    }

    [RelayCommand]
    private async Task VerifyCatalogAsync()
    {
        if (SelectedUser is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار مستخدم أولاً");
            return;
        }

        try
        {
            IsBusy = true;
            var permissions = await _authService.GetUserPermissionsAsync(SelectedUser.Id);
            var report = PermissionCatalogHelper.AnalyzeCoverage(permissions);
            BeautifulMessageDialog.ShowInfo(report.ToDisplayMessage(SelectedUser.DisplayName));
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var s in Screens)
        {
            if (s.ScreenName == ScreenPermissionRegistry.Dashboard)
            {
                s.CanView = true;
                continue;
            }
            s.CanView = false;
            s.CanAdd = false;
            s.CanEdit = false;
            s.CanDelete = false;
            s.CanPrint = false;
            s.CanExport = false;
            s.CanEditPrice = false;
            s.IsViewOnly = false;
        }
    }

    public override async Task InitializeAsync()
    {
        await LoadUsersAsync();

        if (UserNavigationBridge.PendingPermissionsUserId is int pendingId)
        {
            UserNavigationBridge.PendingPermissionsUserId = null;
            SelectedUser = Users.FirstOrDefault(u => u.Id == pendingId);
        }
    }
}

public partial class ScreenPermissionRow : ObservableObject
{
    public string ScreenName { get; set; } = string.Empty;
    public string ScreenLabel { get; set; } = string.Empty;

    [ObservableProperty] private bool _canView;
    [ObservableProperty] private bool _canAdd;
    [ObservableProperty] private bool _canEdit;
    [ObservableProperty] private bool _canDelete;
    [ObservableProperty] private bool _canPrint;
    [ObservableProperty] private bool _canExport;
    [ObservableProperty] private bool _canEditPrice;
    [ObservableProperty] private bool _isViewOnly;

    partial void OnCanViewChanged(bool value)
    {
        if (value || ScreenName == ScreenPermissionRegistry.Dashboard) return;
        CanAdd = false;
        CanEdit = false;
        CanDelete = false;
        CanPrint = false;
        CanExport = false;
        CanEditPrice = false;
        IsViewOnly = false;
    }
}
