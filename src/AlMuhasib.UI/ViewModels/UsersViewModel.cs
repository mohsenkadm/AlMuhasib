using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class UsersViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IExportService _exportService;
    private readonly IBranchService _branchService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;

    public UsersViewModel(
        IAuthService authService,
        IExportService exportService,
        IBranchService branchService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _authService = authService;
        _exportService = exportService;
        _branchService = branchService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "المستخدمون";
    }

    public ObservableCollection<UserRow> Users { get; } = [];
    public ObservableCollection<UserBranchAccessItem> BranchAccessItems { get; } = [];

    [ObservableProperty] private UserRow? _selectedUser;

    // ── Form Fields ─────────────────────────────────────

    [ObservableProperty] private string _formUsername = string.Empty;
    [ObservableProperty] private string _formFullName = string.Empty;
    [ObservableProperty] private string _formPassword = string.Empty;
    [ObservableProperty] private UserRole _formRole = UserRole.User;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _canAssignBranches = true;
    [ObservableProperty] private string _selectedBranchesSummary = "لم يُحدد أي فرع";

    private int _editingUserId;
    private List<UserRow> _allUsers = [];
    private List<Branch> _activeBranches = [];

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

            // عرض الفروع المربوطة يدوياً فقط (وليس توسعة صلاحية «كل الفروع»)
            foreach (var row in _allUsers)
            {
                var branches = await _branchService.GetAssignedBranchesForUserAsync(row.Id);
                row.BranchesDisplay = branches.Count == 0
                    ? "—"
                    : string.Join("، ", branches.Select(b => b.Name));
            }

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

    private async Task LoadBranchAccessOptionsAsync(int? userId = null)
    {
        _activeBranches = (await _branchService.GetActiveAsync()).ToList();
        CanAssignBranches = _currentUserService.IsAdmin
            || _currentUserService.CanEdit(BranchPermissionScreens.AssignUserBranches)
            || _currentUserService.CanView(BranchPermissionScreens.AssignUserBranches);

        HashSet<int> assignedIds = [];
        int? defaultId = null;

        if (userId is > 0)
        {
            // مهم: الربط اليدوي فقط — لا تستخدم GetBranchesForUserAsync لأنها قد تُوسَّع لاحقاً بصلاحيات أخرى
            var assigned = await _branchService.GetAssignedBranchesForUserAsync(userId.Value);
            assignedIds = assigned.Select(b => b.Id).ToHashSet();
            defaultId = await _branchService.GetDefaultBranchIdForUserAsync(userId.Value);
        }

        BranchAccessItems.Clear();
        foreach (var branch in _activeBranches.OrderByDescending(b => b.IsMain).ThenBy(b => b.Name))
        {
            // إضافة مستخدم جديد: الفرع الرئيسي فقط (أو الوحيد إن وُجد فرع واحد)
            var isSelected = userId is > 0
                ? assignedIds.Contains(branch.Id)
                : branch.IsMain || _activeBranches.Count == 1;

            var item = new UserBranchAccessItem
            {
                BranchId = branch.Id,
                Name = branch.Name,
                Code = branch.Code,
                IsMain = branch.IsMain
            };
            item.SetSelectedSilent(isSelected);
            item.SetDefaultSilent(isSelected && (
                (defaultId.HasValue && defaultId.Value == branch.Id)
                || (!defaultId.HasValue && (branch.IsMain || _activeBranches.Count == 1))));
            item.SelectionChanged += OnBranchAccessSelectionChanged;
            BranchAccessItems.Add(item);
        }

        EnsureSingleDefaultBranch();
        RefreshSelectedBranchesSummary();
    }

    private void OnBranchAccessSelectionChanged(UserBranchAccessItem item)
    {
        if (!item.IsSelected && item.IsDefault)
            item.SetDefaultSilent(false);

        if (item.IsDefault && item.IsSelected)
        {
            foreach (var other in BranchAccessItems.Where(b => b != item && b.IsDefault))
                other.SetDefaultSilent(false);
        }

        EnsureSingleDefaultBranch();
        RefreshSelectedBranchesSummary();
    }

    private void EnsureSingleDefaultBranch()
    {
        var selected = BranchAccessItems.Where(b => b.IsSelected).ToList();
        if (selected.Count == 0)
        {
            foreach (var b in BranchAccessItems.Where(b => b.IsDefault))
                b.SetDefaultSilent(false);
            return;
        }

        if (selected.Count(b => b.IsDefault) == 1)
            return;

        foreach (var b in BranchAccessItems.Where(b => b.IsDefault))
            b.SetDefaultSilent(false);

        var preferred = selected.FirstOrDefault(b => b.IsMain) ?? selected[0];
        preferred.SetDefaultSilent(true);
    }

    private void RefreshSelectedBranchesSummary()
    {
        var selected = BranchAccessItems.Where(b => b.IsSelected).ToList();
        if (selected.Count == 0)
        {
            SelectedBranchesSummary = "يجب اختيار فرع واحد على الأقل";
            return;
        }

        var names = selected.Select(b =>
            b.IsDefault ? $"{b.Name} (افتراضي)" : b.Name);
        SelectedBranchesSummary = string.Join(" · ", names);
    }

    [RelayCommand]
    private void SetDefaultBranch(UserBranchAccessItem? item)
    {
        if (item is null || !item.IsSelected) return;
        foreach (var b in BranchAccessItems)
            b.SetDefaultSilent(b == item);
        RefreshSelectedBranchesSummary();
    }

    // ── Add User ────────────────────────────────────────

    [RelayCommand]
    private async Task StartAddAsync()
    {
        IsEditing = false;
        _editingUserId = 0;
        FormUsername = string.Empty;
        FormFullName = string.Empty;
        FormPassword = string.Empty;
        FormRole = UserRole.User;
        await LoadBranchAccessOptionsAsync();
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(FormUsername) || string.IsNullOrWhiteSpace(FormFullName))
        {
            BeautifulMessageDialog.ShowWarning("يرجى إدخال اسم المستخدم والاسم الكامل");
            return;
        }

        // لقطة من الاختيارات الحالية فقط — لا تُعاد قراءة القائمة بعد الإنشاء
        var selectedBranchIds = BranchAccessItems
            .Where(b => b.IsSelected)
            .Select(b => b.BranchId)
            .Distinct()
            .ToList();
        if (selectedBranchIds.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار فرع واحد على الأقل للمستخدم");
            return;
        }

        var defaultBranchId = BranchAccessItems.FirstOrDefault(b => b.IsSelected && b.IsDefault)?.BranchId
                              ?? selectedBranchIds[0];
        if (!selectedBranchIds.Contains(defaultBranchId))
            defaultBranchId = selectedBranchIds[0];

        try
        {
            IsBusy = true;

            int userId;
            if (IsEditing)
            {
                userId = _editingUserId;
                await _authService.UpdateUserAsync(userId, FormFullName.Trim(), FormRole);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FormPassword))
                {
                    BeautifulMessageDialog.ShowWarning("يرجى إدخال كلمة المرور");
                    return;
                }

                var created = await _authService.CreateUserAsync(
                    FormUsername.Trim(), FormPassword, FormFullName.Trim(), FormRole);
                userId = created.Id;
            }

            if (CanAssignBranches)
            {
                await _branchService.AssignUserBranchesAsync(userId, selectedBranchIds, defaultBranchId);
            }
            else if (!IsEditing)
            {
                // مستخدم جديد بدون صلاحية الربط — اربطه بالفرع الرئيسي تلقائياً
                await _branchService.EnsureUserLinkedToMainAsync(userId);
            }

            BeautifulMessageDialog.ShowSuccess(IsEditing ? "تم تحديث المستخدم بنجاح" : "تم إضافة المستخدم بنجاح");

            FormUsername = string.Empty;
            FormFullName = string.Empty;
            FormPassword = string.Empty;
            FormRole = UserRole.User;
            IsEditing = false;
            _editingUserId = 0;
            await LoadUsersAsync();
            await LoadBranchAccessOptionsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ── Edit User ───────────────────────────────────────

    [RelayCommand]
    private async Task StartEditAsync()
    {
        if (SelectedUser is null) return;
        IsEditing = true;
        _editingUserId = SelectedUser.Id;
        FormUsername = SelectedUser.Username;
        FormFullName = SelectedUser.FullName;
        FormPassword = string.Empty;
        FormRole = SelectedUser.Role;
        await LoadBranchAccessOptionsAsync(SelectedUser.Id);
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
    private async Task CancelEditAsync()
    {
        IsEditing = false;
        _editingUserId = 0;
        FormUsername = string.Empty;
        FormFullName = string.Empty;
        FormPassword = string.Empty;
        FormRole = UserRole.User;
        await LoadBranchAccessOptionsAsync();
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
            var exportData = new List<object>();
            foreach (var u in users)
            {
                var branches = await _branchService.GetAssignedBranchesForUserAsync(u.Id);
                exportData.Add(new
                {
                    اسم_المستخدم = u.Username,
                    الاسم_الكامل = u.FullName,
                    الصلاحية = u.Role == UserRole.Admin ? "مدير" : "مستخدم",
                    الفروع = branches.Count == 0 ? "—" : string.Join("، ", branches.Select(b => b.Name)),
                    الحالة = u.IsActive ? "فعال" : "معطّل"
                });
            }

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
            var columns = new[] { "اسم المستخدم", "الاسم الكامل", "الصلاحية", "الفروع", "الحالة" };
            var rows = new List<object[]>();
            foreach (var u in users)
            {
                var branches = await _branchService.GetAssignedBranchesForUserAsync(u.Id);
                rows.Add(
                [
                    u.Username,
                    u.FullName,
                    u.Role == UserRole.Admin ? "مدير" : "مستخدم",
                    branches.Count == 0 ? "—" : string.Join("، ", branches.Select(b => b.Name)),
                    u.IsActive ? "فعال" : "معطّل"
                ]);
            }
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
        await LoadBranchAccessOptionsAsync();
        await LoadUsersAsync();
    }
}

public partial class UserBranchAccessItem : ObservableObject
{
    public int BranchId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsMain { get; init; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Code) ? Name : $"{Name} ({Code})";

    public string TypeLabel => IsMain ? "رئيسي" : "فرعي";

    public event Action<UserBranchAccessItem>? SelectionChanged;

    private bool _suppressNotify;

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isDefault;

    public void SetSelectedSilent(bool value)
    {
        _suppressNotify = true;
        IsSelected = value;
        _suppressNotify = false;
    }

    public void SetDefaultSilent(bool value)
    {
        _suppressNotify = true;
        IsDefault = value;
        _suppressNotify = false;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (!_suppressNotify)
            SelectionChanged?.Invoke(this);
    }

    partial void OnIsDefaultChanged(bool value)
    {
        if (!_suppressNotify && value)
            SelectionChanged?.Invoke(this);
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
    public string BranchesDisplay { get; set; } = "—";

    /// <summary>عرض في القوائم المنسدلة (الصلاحيات، المستخدمون، ...)</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(FullName)
            ? $"{Username} — {RoleDisplay}"
            : $"{FullName} ({Username})";

    public override string ToString() => DisplayName;
}
