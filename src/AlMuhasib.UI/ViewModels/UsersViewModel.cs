using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class UsersViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    public UsersViewModel(IAuthService authService)
    {
        _authService = authService;
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
            Users.Clear();
            foreach (var u in users)
            {
                Users.Add(new UserRow
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role,
                    RoleDisplay = u.Role == UserRole.Admin ? "مدير" : "مستخدم",
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
