using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class CloudSyncSettingsViewModel : ViewModelBase
{
    private readonly ICloudSyncSettingsService _settingsService;
    private readonly ISyncService _syncService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private string _apiBaseUrl = string.Empty;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _autoSyncEnabled;
    [ObservableProperty] private int _autoSyncIntervalMinutes = 15;
    [ObservableProperty] private DateTime? _lastSuccessfulSyncAt;
    [ObservableProperty] private string? _lastSyncError;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isAdmin;
    [ObservableProperty] private bool? _isConnectionOk;
    [ObservableProperty] private string _connectionStatusText = "غير مُختبر";
    [ObservableProperty] private int _lastAcceptedCount;
    [ObservableProperty] private int _lastConflictCount;

    public CloudSyncSettingsViewModel(
        ICloudSyncSettingsService settingsService,
        ISyncService syncService,
        ICurrentUserService currentUserService)
    {
        _settingsService = settingsService;
        _syncService = syncService;
        _currentUserService = currentUserService;
        PageTitle = "المزامنة السحابية";
        IsAdmin = currentUserService.Role == Core.Enums.UserRole.Admin;
    }

    public bool HasSyncError => !string.IsNullOrWhiteSpace(LastSyncError);
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage) && !HasSyncError;

    public string LastSyncDisplay =>
        LastSuccessfulSyncAt.HasValue
            ? LastSuccessfulSyncAt.Value.ToLocalTime().ToString("yyyy/MM/dd")
            : "لم تتم بعد";

    public string LastSyncDetail =>
        LastSuccessfulSyncAt.HasValue
            ? $"الساعة {LastSuccessfulSyncAt.Value.ToLocalTime():HH:mm}"
            : "اضغط «مزامنة الآن» لبدء الرفع";

    public string LastConflictDisplay =>
        LastConflictCount > 0 ? $"{LastConflictCount} تعارض" : "بدون تعارضات";

    public string AutoSyncStatusText => AutoSyncEnabled ? "مفعّلة" : "معطّلة";

    public string AutoSyncIntervalDisplay =>
        AutoSyncEnabled ? $"كل {AutoSyncIntervalMinutes} دقيقة" : "تفعيل يدوي فقط";

    public PackIconKind ConnectionIconKind => IsConnectionOk switch
    {
        true => PackIconKind.CloudCheck,
        false => PackIconKind.CloudOffOutline,
        _ => PackIconKind.CloudQuestionOutline
    };

    public override async Task InitializeAsync()
    {
        if (!IsAdmin) return;
        var settings = await _settingsService.GetAsync();
        ApiBaseUrl = settings.ApiBaseUrl;
        Username = settings.Username;
        Password = settings.Password;
        AutoSyncEnabled = settings.AutoSyncEnabled;
        AutoSyncIntervalMinutes = settings.AutoSyncIntervalMinutes;
        LastSuccessfulSyncAt = settings.LastSuccessfulSyncAt;
        LastSyncError = settings.LastSyncError;
    }

    partial void OnLastSyncErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasSyncError));
        OnPropertyChanged(nameof(HasStatusMessage));
    }
    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));
    partial void OnLastSuccessfulSyncAtChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(LastSyncDisplay));
        OnPropertyChanged(nameof(LastSyncDetail));
    }
    partial void OnAutoSyncEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoSyncStatusText));
        OnPropertyChanged(nameof(AutoSyncIntervalDisplay));
    }
    partial void OnAutoSyncIntervalMinutesChanged(int value) => OnPropertyChanged(nameof(AutoSyncIntervalDisplay));
    partial void OnLastAcceptedCountChanged(int value) => OnPropertyChanged(nameof(LastConflictDisplay));
    partial void OnLastConflictCountChanged(int value) => OnPropertyChanged(nameof(LastConflictDisplay));
    partial void OnIsConnectionOkChanged(bool? value) => OnPropertyChanged(nameof(ConnectionIconKind));

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!IsAdmin) return;
        try
        {
            IsBusy = true;
            var settings = await _settingsService.GetAsync();
            settings.ApiBaseUrl = ApiBaseUrl.Trim();
            settings.Username = Username.Trim();
            settings.Password = Password;
            settings.AutoSyncEnabled = AutoSyncEnabled;
            settings.AutoSyncIntervalMinutes = AutoSyncIntervalMinutes;
            await _settingsService.SaveAsync(settings);
            StatusMessage = "تم حفظ الإعدادات بنجاح";
            LastSyncError = null;
            if (AutoSyncEnabled)
                await _syncService.StartAutoSyncAsync();
            else
                _syncService.StopAutoSync();
        }
        catch (Exception ex)
        {
            StatusMessage = string.Empty;
            LastSyncError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (!IsAdmin) return;
        try
        {
            IsBusy = true;
            await SaveSettingsOnlyAsync();
            var status = await _syncService.TestConnectionAsync();
            IsConnectionOk = status.IsSuccess;
            ConnectionStatusText = status.IsSuccess ? "متصل" : "غير متصل";
            StatusMessage = status.IsSuccess ? "الاتصال ناجح — الترخيص فعّال" : status.Message;
            if (!status.IsSuccess)
                LastSyncError = status.Message;
        }
        catch (Exception ex)
        {
            IsConnectionOk = false;
            ConnectionStatusText = "فشل الاتصال";
            StatusMessage = string.Empty;
            LastSyncError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        if (!IsAdmin) return;
        try
        {
            IsBusy = true;
            await SaveSettingsOnlyAsync();
            var result = await _syncService.SyncNowAsync();
            var settings = await _settingsService.GetAsync();
            LastSuccessfulSyncAt = settings.LastSuccessfulSyncAt;
            LastSyncError = settings.LastSyncError;
            LastAcceptedCount = result.AcceptedCount;
            LastConflictCount = result.ConflictCount;
            IsConnectionOk = true;
            ConnectionStatusText = "متصل";
            StatusMessage = result.Message;
        }
        catch (Exception ex)
        {
            IsConnectionOk = false;
            ConnectionStatusText = "فشل المزامنة";
            StatusMessage = string.Empty;
            LastSyncError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSettingsOnlyAsync()
    {
        var settings = await _settingsService.GetAsync();
        settings.ApiBaseUrl = ApiBaseUrl.Trim();
        settings.Username = Username.Trim();
        settings.Password = Password;
        settings.AutoSyncEnabled = AutoSyncEnabled;
        settings.AutoSyncIntervalMinutes = AutoSyncIntervalMinutes;
        await _settingsService.SaveAsync(settings);
    }
}
