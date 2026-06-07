using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly CurrentUserService _currentUserService;
    private readonly ISoundService _sound;

    public ObservableCollection<LoginAdminOption> AdminUsers { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectingAdmin))]
    [NotifyPropertyChangedFor(nameof(IsEnteringPassword))]
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

    public bool IsSelectingAdmin => CurrentStep == LoginStep.SelectAdmin;
    public bool IsEnteringPassword => CurrentStep == LoginStep.EnterPassword;

    public event Action? LoginSucceeded;
    public event Action? StepChanged;

    public LoginViewModel(IAuthService authService, CurrentUserService currentUserService, ISoundService sound)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _sound = sound;
    }

    public async Task LoadAdminsAsync()
    {
        IsLoadingAdmins = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var admins = await _authService.GetActiveAdminUsersAsync();
            AdminUsers.Clear();

            var index = 0;
            foreach (var admin in admins)
                AdminUsers.Add(LoginAdminOption.FromUser(admin, index++));

            HasAdmins = AdminUsers.Count > 0;
            if (!HasAdmins)
            {
                ShowError("لا يوجد حساب مدير نشط. يرجى مراجعة إعدادات المستخدمين.");
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
        CurrentStep = LoginStep.EnterPassword;
        StepChanged?.Invoke();
    }

    [RelayCommand]
    private void BackToAdminSelection()
    {
        Password = string.Empty;
        HasError = false;
        ErrorMessage = string.Empty;
        CurrentStep = LoginStep.SelectAdmin;
        StepChanged?.Invoke();
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

            _sound.Play(SoundEffect.Login);
            LoginSucceeded?.Invoke();
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

    partial void OnCurrentStepChanged(LoginStep value) => StepChanged?.Invoke();

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
