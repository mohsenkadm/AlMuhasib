using System.Windows;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly CurrentUserService _currentUserService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Raised when login succeeds. The Window subscribes to close itself.
    /// </summary>
    public event Action? LoginSucceeded;

    public LoginViewModel(IAuthService authService, CurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ShowError("يرجى إدخال اسم المستخدم وكلمة المرور");
            return;
        }

        IsLoading = true;

        try
        {
            var result = await _authService.LoginAsync(Username.Trim(), Password);

            if (!result.Success)
            {
                ShowError(result.ErrorMessage);
                return;
            }

            // Store the authenticated user in the singleton service
            _currentUserService.Username = result.User!.Username;
            _currentUserService.UserId = result.User.Id;
            _currentUserService.Role = result.User.Role;

            // TODO: Handle MustChangePassword — navigate to password change dialog
            if (result.MustChangePassword)
            {
                // For now, just log in; force-change can be added later
            }

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

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
