using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using System.Diagnostics;
using System.IO;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly CurrentUserService _currentUserService;
    private readonly IBranchService _branchService;
    private readonly IBranchContext _branchContext;
    private readonly ISoundService _sound;

    public ObservableCollection<LoginAdminOption> AdminUsers { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectingAdmin))]
    [NotifyPropertyChangedFor(nameof(IsEnteringPassword))]
    [NotifyPropertyChangedFor(nameof(IsSelectingBranch))]
    private LoginStep _currentStep = LoginStep.SelectAdmin;

    [ObservableProperty]
    private LoginAdminOption? _selectedAdmin;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoadingAdmins;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _hasAdmins;

    public ObservableCollection<Branch> AvailableBranches { get; } = [];

    [ObservableProperty]
    private Branch? _selectedBranch;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectingAdmin))]
    [NotifyPropertyChangedFor(nameof(IsEnteringPassword))]
    [NotifyPropertyChangedFor(nameof(IsSelectingBranch))]
    private bool _showBranchPicker;

    public bool IsSelectingAdmin => CurrentStep == LoginStep.SelectAdmin && !ShowBranchPicker;
    public bool IsEnteringPassword => CurrentStep == LoginStep.EnterPassword && !ShowBranchPicker;
    public bool IsSelectingBranch => ShowBranchPicker;

    public event Action? LoginSucceeded;
    public event Action? StepChanged;

    public LoginViewModel(
        IAuthService authService,
        CurrentUserService currentUserService,
        IBranchService branchService,
        IBranchContext branchContext,
        ISoundService sound)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _branchService = branchService;
        _branchContext = branchContext;
        _sound = sound;
    }

    public async Task LoadAdminsAsync()
    {
        IsLoadingAdmins = true;
        HasError = false;
        ErrorMessage = string.Empty;
        ResetBranchPickerState();

        try
        {
            var users = await _authService.GetActiveUsersAsync();
            AdminUsers.Clear();

            var index = 0;
            foreach (var user in users)
                AdminUsers.Add(LoginAdminOption.FromUser(user, index++));

            HasAdmins = AdminUsers.Count > 0;
            if (!HasAdmins)
            {
                ShowError("لا يوجد مستخدم نشط. يرجى مراجعة إعدادات المستخدمين.");
                return;
            }

            CurrentStep = LoginStep.SelectAdmin;
            SelectedAdmin = null;
            Password = string.Empty;
        }
        catch (Exception ex)
        {
            ShowError($"تعذر تحميل حسابات المديرين: {ex.Message}");
        }
        finally
        {
            IsLoadingAdmins = false;
        }
    }

    [RelayCommand]
    private void SelectAdmin(LoginAdminOption? admin)
    {
        if (admin is null) return;

        SelectedAdmin = admin;
        Password = string.Empty;
        HasError = false;
        ErrorMessage = string.Empty;
        ResetBranchPickerState();
        CurrentStep = LoginStep.EnterPassword;
        StepChanged?.Invoke();
    }

    [RelayCommand]
    private void BackToAdminSelection()
    {
        Password = string.Empty;
        HasError = false;
        ErrorMessage = string.Empty;
        ResetBranchPickerState();
        CurrentStep = LoginStep.SelectAdmin;
        StepChanged?.Invoke();
    }

    [RelayCommand]
    private void BackToPassword()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        ResetBranchPickerState();
        CurrentStep = LoginStep.EnterPassword;
        StepChanged?.Invoke();
    }

    [RelayCommand]
    private void OpenOnScreenKeyboard()
    {
        try
        {
            var osk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "osk.exe");
            Process.Start(new ProcessStartInfo(osk) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذّر فتح لوحة المفاتيح:\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (SelectedAdmin is null)
        {
            ShowError("يرجى اختيار حساب المدير أولاً");
            CurrentStep = LoginStep.SelectAdmin;
            StepChanged?.Invoke();
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError("يرجى إدخال كلمة المرور");
            return;
        }

        IsLoading = true;

        try
        {
            var result = await _authService.LoginAsync(SelectedAdmin.Username, Password);

            if (!result.Success)
            {
                _sound.Play(SoundEffect.Error);
                ShowError(result.ErrorMessage);
                return;
            }

            _currentUserService.Username = result.User!.Username;
            _currentUserService.UserId = result.User.Id;
            _currentUserService.Role = result.User.Role;

            // مصدر الحقيقة الوحيد = الفروع المربوطة يدوياً في شاشة المستخدمين.
            // لا نوسّع أبداً إلى كل الفروع النشطة بسبب صلاحية «عرض/إدارة كل الفروع».
            var assigned = await _branchService.GetAssignedBranchesForUserAsync(result.User.Id);
            IReadOnlyList<Branch> branches = assigned;

            // إصلاح خلفي فقط: شركة بفرع واحد نشط بلا صف UserBranches.
            if (branches.Count == 0)
                branches = await _branchService.GetBranchesForUserAsync(result.User.Id);

            if (branches.Count == 0)
            {
                _sound.Play(SoundEffect.Error);
                ShowError("لا يوجد فرع مرتبط بهذا المستخدم. اربطه بفرع واحد على الأقل من شاشة المستخدمين.");
                return;
            }

            // وضع «كل الفروع» للتقارير يعمل فقط عبر الفروع المربوطة، ويتطلب أكثر من فرع.
            var hasViewAllPerm = await _authService.HasPermissionAsync(
                result.User.Id, BranchPermissionScreens.ViewAllBranches, "View");
            var hasManageAllPerm = await _authService.HasPermissionAsync(
                result.User.Id, BranchPermissionScreens.ManageAllBranches, "View");
            var canViewAll = branches.Count > 1 && (hasViewAllPerm || hasManageAllPerm);
            var canManageAll = branches.Count > 1 && hasManageAllPerm;

            _branchContext.SetAllowedBranches(
                branches.Select(b => b.Id),
                canViewAll: canViewAll,
                canManageAll: canManageAll);

            // فرع واحد مربوط → دخول مباشر لهذا الفرع فقط، بدون شاشة اختيار.
            if (branches.Count == 1)
            {
                await BindBranchAndFinishAsync(branches[0]);
                return;
            }

            // أكثر من فرع مربوط → اختيار من الفروع المربوطة فقط.
            AvailableBranches.Clear();
            foreach (var b in branches)
                AvailableBranches.Add(b);

            var defaultId = await _branchService.GetDefaultBranchIdForUserAsync(result.User.Id);
            SelectedBranch = branches.FirstOrDefault(b => b.Id == defaultId)
                             ?? branches[0];

            ShowBranchPicker = true;
            StepChanged?.Invoke();
        }
        catch (Exception ex)
        {
            ShowError($"حدث خطأ أثناء تسجيل الدخول: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmBranchAsync()
    {
        if (SelectedBranch is null)
        {
            ShowError("يرجى اختيار الفرع");
            return;
        }

        await BindBranchAndFinishAsync(SelectedBranch);
    }

    [RelayCommand]
    private async Task SelectBranchAndContinueAsync(Branch? branch)
    {
        if (branch is null) return;
        SelectedBranch = branch;
        await BindBranchAndFinishAsync(branch);
    }

    private async Task BindBranchAndFinishAsync(Branch branch)
    {
        if (_branchContext.AllowedBranchIds.Count > 0
            && !_branchContext.AllowedBranchIds.Contains(branch.Id))
        {
            _sound.Play(SoundEffect.Error);
            ShowError("لا تملك صلاحية الدخول إلى هذا الفرع.");
            return;
        }

        try
        {
            _branchContext.SetCurrentBranch(branch.Id, branch.Name, branch.Code);
        }
        catch (UnauthorizedAccessException)
        {
            _sound.Play(SoundEffect.Error);
            ShowError("لا تملك صلاحية الدخول إلى هذا الفرع.");
            return;
        }

        ShowBranchPicker = false;
        AvailableBranches.Clear();
        SelectedBranch = null;
        _sound.Play(SoundEffect.Login);
        LoginSucceeded?.Invoke();
        await Task.CompletedTask;
    }

    private void ResetBranchPickerState()
    {
        ShowBranchPicker = false;
        AvailableBranches.Clear();
        SelectedBranch = null;
    }

    partial void OnCurrentStepChanged(LoginStep value) => StepChanged?.Invoke();

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
