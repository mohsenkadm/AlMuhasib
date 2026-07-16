using System.Collections.ObjectModel;
using System.Windows;
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
    [ObservableProperty] private double _syncProgressPercent;
    [ObservableProperty] private string _syncProgressMessage = string.Empty;
    [ObservableProperty] private string _syncRemainingText = string.Empty;
    [ObservableProperty] private string _diagnosticsText = string.Empty;
    [ObservableProperty] private string _copyFeedback = string.Empty;

    public ObservableCollection<SyncConflictInfo> Conflicts { get; } = [];

    /// <summary>True when the user typed in PasswordBox before/during async settings load.</summary>
    public bool PasswordEditedByUser { get; set; }

    public CloudSyncSettingsViewModel(
        ICloudSyncSettingsService settingsService,
        ISyncService syncService,
        ICurrentUserService currentUserService)
    {
        _settingsService = settingsService;
        _syncService = syncService;
        _currentUserService = currentUserService;
        PageTitle = "المزامنة السحابية";
        IsAdmin = currentUserService.CanView("CloudSync");
        LoadPermissions(currentUserService, "CloudSync");
    }

    /// <summary>Fired after settings are loaded from the database so the view can sync PasswordBox.</summary>
    public event Action? SettingsLoaded;

    public bool HasSyncError => !string.IsNullOrWhiteSpace(LastSyncError);
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage) && !HasSyncError && !HasConflicts;
    public bool HasConflicts => Conflicts.Count > 0;
    public bool CanCopyDiagnostics => !string.IsNullOrWhiteSpace(DiagnosticsText);
    public bool HasCopyFeedback => !string.IsNullOrWhiteSpace(CopyFeedback);

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
        var settings = await _settingsService.GetAsync();
        ApiBaseUrl = settings.ApiBaseUrl;
        Username = settings.Username;
        // Don't wipe a password the user already typed while this async load was in flight.
        if (!PasswordEditedByUser)
            Password = settings.Password;
        AutoSyncEnabled = settings.AutoSyncEnabled;
        AutoSyncIntervalMinutes = settings.AutoSyncIntervalMinutes;
        LastSuccessfulSyncAt = settings.LastSuccessfulSyncAt;
        LastSyncError = settings.LastSyncError;
        SettingsLoaded?.Invoke();
    }

    partial void OnLastSyncErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasSyncError));
        OnPropertyChanged(nameof(HasStatusMessage));
    }
    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));
    partial void OnDiagnosticsTextChanged(string value) => OnPropertyChanged(nameof(CanCopyDiagnostics));
    partial void OnCopyFeedbackChanged(string value) => OnPropertyChanged(nameof(HasCopyFeedback));
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
        if (!CanEdit) return;
        try
        {
            IsBusy = true;
            SyncProgressMessage = "جاري حفظ الإعدادات...";
            SyncProgressPercent = 50;
            SyncRemainingText = string.Empty;
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
            SyncProgressPercent = 0;
            SyncProgressMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (!CanEdit) return;
        try
        {
            IsBusy = true;
            SyncProgressMessage = "جاري اختبار الاتصال...";
            SyncProgressPercent = 40;
            SyncRemainingText = string.Empty;
            await SaveSettingsOnlyAsync();
            var status = await _syncService.TestConnectionAsync();
            IsConnectionOk = status.IsSuccess;
            ConnectionStatusText = status.IsSuccess ? "متصل" : "غير متصل";
            StatusMessage = status.IsSuccess ? "الاتصال ناجح — الترخيص فعّال" : status.Message;
            if (!status.IsSuccess)
                LastSyncError = status.Message;
            else
                LastSyncError = null;
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
            SyncProgressPercent = 0;
            SyncProgressMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        if (!CanEdit) return;
        try
        {
            IsBusy = true;
            Conflicts.Clear();
            DiagnosticsText = string.Empty;
            CopyFeedback = string.Empty;
            LastSyncError = null;
            SyncProgressPercent = 0;
            SyncProgressMessage = "بدء المزامنة...";
            SyncRemainingText = "متبقي 6 من 6";
            OnPropertyChanged(nameof(HasConflicts));
            OnPropertyChanged(nameof(HasStatusMessage));

            await SaveSettingsOnlyAsync();

            var progress = new Progress<SyncProgressUpdate>(update =>
            {
                SyncProgressPercent = update.Percent;
                SyncProgressMessage = update.Message;
                SyncRemainingText = $"متبقي {update.RemainingSteps} من {update.TotalSteps}";
            });

            var result = await _syncService.SyncNowAsync(progress);
            var settings = await _settingsService.GetAsync();
            LastSuccessfulSyncAt = settings.LastSuccessfulSyncAt;
            LastSyncError = settings.LastSyncError;
            LastAcceptedCount = result.AcceptedCount;
            LastConflictCount = result.ConflictCount;
            DiagnosticsText = result.DiagnosticsText;
            Conflicts.Clear();
            foreach (var c in result.Conflicts)
                Conflicts.Add(c);

            OnPropertyChanged(nameof(HasConflicts));
            OnPropertyChanged(nameof(HasStatusMessage));

            IsConnectionOk = true;
            ConnectionStatusText = "متصل";
            StatusMessage = result.IsSuccess ? result.Message : string.Empty;
            if (!result.IsSuccess && string.IsNullOrWhiteSpace(LastSyncError))
                LastSyncError = result.Message;
        }
        catch (Exception ex)
        {
            IsConnectionOk = false;
            ConnectionStatusText = "فشل المزامنة";
            StatusMessage = string.Empty;
            LastSyncError = ex.Message;
            DiagnosticsText =
                $"=== فشل المزامنة ==={Environment.NewLine}" +
                $"TimeUtc: {DateTime.UtcNow:O}{Environment.NewLine}" +
                $"ApiBaseUrl: {ApiBaseUrl}{Environment.NewLine}" +
                $"Username: {Username}{Environment.NewLine}" +
                $"Error: {ex}{Environment.NewLine}";
        }
        finally
        {
            IsBusy = false;
            if (SyncProgressPercent < 100 && string.IsNullOrWhiteSpace(LastSyncError) && !HasConflicts)
            {
                SyncProgressPercent = 100;
                SyncProgressMessage = "اكتملت";
                SyncRemainingText = "متبقي 0 من 6";
            }
        }
    }

    [RelayCommand]
    private void CopyDiagnostics()
    {
        if (string.IsNullOrWhiteSpace(DiagnosticsText))
            return;

        try
        {
            Clipboard.SetText(DiagnosticsText);
            CopyFeedback = "تم نسخ التشخيص — الصقه في المحادثة لإصلاح التعارضات";
        }
        catch (Exception ex)
        {
            CopyFeedback = $"تعذر النسخ: {ex.Message}";
        }
    }

    private async Task SaveSettingsOnlyAsync()
    {
        var settings = await _settingsService.GetAsync();
        var credentialsChanged =
            !string.Equals(settings.ApiBaseUrl?.Trim(), ApiBaseUrl.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(settings.Username?.Trim(), Username.Trim(), StringComparison.Ordinal) ||
            settings.Password != Password;

        settings.ApiBaseUrl = ApiBaseUrl.Trim();
        settings.Username = Username.Trim();
        settings.Password = Password;
        settings.AutoSyncEnabled = AutoSyncEnabled;
        settings.AutoSyncIntervalMinutes = AutoSyncIntervalMinutes;

        // Avoid reusing a token issued for previous credentials/URL.
        if (credentialsChanged)
        {
            settings.AccessToken = string.Empty;
            settings.RefreshToken = string.Empty;
            settings.AccessTokenExpiresAt = null;
        }

        await _settingsService.SaveAsync(settings);
    }
}
