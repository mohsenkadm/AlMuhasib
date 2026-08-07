using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class UsersViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IExportService _exportService;
    private readonly MainWindowViewModel _mainWindow;

    public UsersViewModel(IAuthService authService, IExportService exportService, MainWindowViewModel mainWindow)
    {
        _authService = authService;
        _exportService = exportService;
        _mainWindow = mainWindow;
        PageTitle = "المستخدمون";
    }

    public ObservableCollection<UserRow> Users { get; } = [];

    [ObservableProperty] private UserRow? _selectedUser;

    // ── Form Fields ─────────────────────────────────────

    [ObservableProperty] private string _formUsername = string.Empty;
    [ObservableProperty] private string _formFullName = string.Empty;
    [ObservableProperty] private string _formPassword = string.Empty;
    [ObservableProperty] private UserRole _formRole = UserRole.User;
    [ObservableProperty] private bool _isEditing;

    private int _editingUserId;
    private List<UserRow> _allUsers = [];

    // ── Reset Password Fields ───────────────────────────

    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private bool _isResetPasswordOpen;

    // ── Load ────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;
            var users = await _authService.GetAllUsersAsync();
            _allUsers = users.Select(u => new UserRow
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                RoleDisplay = u.Role == UserRole.Admin ? "مدير" : "مستخدم",
                IsActive = u.IsActive,
                StatusDisplay = u.IsActive ? "فعال" : "معطّل"
            }).ToList();

            ApplyUserFilters();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    private void ApplyUserFilters()
    {
        var filtered = ColumnFilterEngine.Apply(_allUsers, ColumnFilters);
        Users.Clear();
        foreach (var u in filtered)
            Users.Add(u);
    }

    protected override void OnColumnFiltersChanged() => ApplyUserFilters();

    // ── Add User ────────────────────────────────────────

    [RelayCommand]
    private void StartAdd()
    {
        IsEditing = false;
        _editingUserId = 0;
        FormUsername = string.Empty;
        FormFullName = string.Empty;
        FormPassword = string.Empty;
        FormRole = UserRole.User;
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(FormUsername) || string.IsNullOrWhiteSpace(FormFullName))
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال اسم المستخدم والاسم الكامل");
            return;
        }

        try
        {
            IsBusy = true;

            if (IsEditing)
            {
                await _authService.UpdateUserAsync(_editingUserId, FormFullName.Trim(), FormRole);
                BeautifulMessageDialog.ShowSuccess("تم تحديث المستخدم بنجاح");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FormPassword))
                {
                    BeautifulMessageDialog.ShowWarning("يرجى إدخال كلمة المرور");
                    return;
                }
                await _authService.CreateUserAsync(FormUsername.Trim(), FormPassword, FormFullName.Trim(), FormRole);
                BeautifulMessageDialog.ShowSuccess("تم إضافة المستخدم بنجاح");
            }

            FormUsername = string.Empty;
            FormFullName = string.Empty;
            FormPassword = string.Empty;
            FormRole = UserRole.User;
            IsEditing = false;
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ── Edit User ───────────────────────────────────────

    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedUser is null) return;
        IsEditing = true;
        _editingUserId = SelectedUser.Id;
        FormUsername = SelectedUser.Username;
        FormFullName = SelectedUser.FullName;
        FormPassword = string.Empty;
        FormRole = SelectedUser.Role;
    }

    // ── Toggle Active ───────────────────────────────────

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedUser is null) return;
        try
        {
            IsBusy = true;
            bool newState = !SelectedUser.IsActive;
            await _authService.SetUserActiveAsync(SelectedUser.Id, newState);
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ── Reset Password ──────────────────────────────────

    [RelayCommand]
    private void OpenResetPassword()
    {
        if (SelectedUser is null) return;
        NewPassword = string.Empty;
        IsResetPasswordOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmResetPasswordAsync()
    {
        if (SelectedUser is null || string.IsNullOrWhiteSpace(NewPassword)) return;
        try
        {
            IsBusy = true;
            await _authService.ResetPasswordAsync(SelectedUser.Id, NewPassword);
            IsResetPasswordOpen = false;
            NewPassword = string.Empty;
            BeautifulMessageDialog.ShowSuccess("تم إعادة تعيين كلمة المرور");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelResetPassword()
    {
        IsResetPasswordOpen = false;
        NewPassword = string.Empty;
    }

    // ── Cancel Edit ─────────────────────────────────────

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        _editingUserId = 0;
        FormUsername = string.Empty;
        FormFullName = string.Empty;
        FormPassword = string.Empty;
        FormRole = UserRole.User;
    }

    [RelayCommand]
    private async Task OpenUserProfileAsync(UserRow? row)
    {
        var user = row ?? SelectedUser;
        if (user is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار مستخدم");
            return;
        }

        UserNavigationBridge.PendingActivityUserId = user.Id;
        var title = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
        await _mainWindow.OpenTabAsync(
            typeof(UserActivityProfileViewModel),
            $"ملف — {title}",
            MaterialDesignThemes.Wpf.PackIconKind.AccountCircle,
            activateIfExists: false);
    }

    [RelayCommand]
    private async Task OpenUserPermissionsAsync(UserRow? row)
    {
        var user = row ?? SelectedUser;
        if (user is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار مستخدم");
            return;
        }

        UserNavigationBridge.PendingPermissionsUserId = user.Id;
        var title = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
        await _mainWindow.OpenTabAsync(
            typeof(PermissionsViewModel),
            $"صلاحيات — {title}",
            MaterialDesignThemes.Wpf.PackIconKind.ShieldAccount,
            activateIfExists: false);
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var users = await _authService.GetAllUsersAsync();
            var exportData = users.Select(u => new
            {
                اسم_المستخدم = u.Username,
                الاسم_الكامل = u.FullName,
                الصلاحية = u.Role == UserRole.Admin ? "مدير" : "مستخدم",
                الحالة = u.IsActive ? "فعال" : "معطّل"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"المستخدمون_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المستخدمون");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            var users = await _authService.GetAllUsersAsync();
            var columns = new[] { "اسم المستخدم", "الاسم الكامل", "الصلاحية", "الحالة" };
            IList<object[]> rows = users.Select(u => new object[]
            {
                u.Username,
                u.FullName,
                u.Role == UserRole.Admin ? "مدير" : "مستخدم",
                u.IsActive ? "فعال" : "معطّل"
            }).ToList();
            _exportService.PrintTable("قائمة المستخدمين", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    // ── Init ────────────────────────────────────────────

    public override async Task InitializeAsync()
    {
        await LoadUsersAsync();
    }
}

// ── Display Model ───────────────────────────────────────

public class UserRow
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleDisplay { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;

    /// <summary>عرض في القوائم المنسدلة (الصلاحيات، المستخدمون، ...)</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(FullName)
            ? $"{Username} — {RoleDisplay}"
            : $"{FullName} ({Username})";

    public override string ToString() => DisplayName;
}
