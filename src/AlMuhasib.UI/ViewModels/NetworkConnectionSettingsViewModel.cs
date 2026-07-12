using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Network;
using AlMuhasib.Infrastructure.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class NetworkConnectionSettingsViewModel : ViewModelBase
{
    private readonly ISystemProfileService _systemProfile;
    private readonly INetworkConnectionService _networkConnectionService;
    private readonly IMainServerHostingService _mainServerHostingService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private string _deploymentModeText = string.Empty;
    [ObservableProperty] private string _mainServerHost = string.Empty;
    [ObservableProperty] private int _sqlPort = 1433;
    [ObservableProperty] private string _sqlInstance = "SQLEXPRESS";
    [ObservableProperty] private string _sqlUsername = string.Empty;
    [ObservableProperty] private string _sqlPassword = string.Empty;
    [ObservableProperty] private string _pairingCode = string.Empty;
    [ObservableProperty] private string _branchDisplayName = string.Empty;
    [ObservableProperty] private string _serverLabel = string.Empty;
    [ObservableProperty] private string _mainPairingCode = string.Empty;
    [ObservableProperty] private bool _allowBranchConnections;
    [ObservableProperty] private bool _discoveryEnabled = true;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool? _isConnectionOk;
    [ObservableProperty] private string _connectionStatusText = "غير مُختبر";
    [ObservableProperty] private int? _latencyMs;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _isBranchClient;
    [ObservableProperty] private bool _isMainServer;
    [ObservableProperty] private bool _showMainServerSettings;
    [ObservableProperty] private bool _isAdmin;

    public ObservableCollection<DiscoveredMainServer> DiscoveredServers { get; } = [];

    [ObservableProperty] private DiscoveredMainServer? _selectedDiscoveredServer;

    public NetworkConnectionSettingsViewModel(
        ISystemProfileService systemProfile,
        INetworkConnectionService networkConnectionService,
        IMainServerHostingService mainServerHostingService,
        ICurrentUserService currentUserService)
    {
        _systemProfile = systemProfile;
        _networkConnectionService = networkConnectionService;
        _mainServerHostingService = mainServerHostingService;
        _currentUserService = currentUserService;
        PageTitle = "ربط الحاسبات";
        IsAdmin = currentUserService.IsAdmin;
        LoadPermissions(currentUserService, ScreenPermissionRegistry.NetworkConnection);
    }

    public PackIconKind ConnectionIconKind => IsConnectionOk switch
    {
        true => PackIconKind.LanConnect,
        false => PackIconKind.LanDisconnect,
        _ => PackIconKind.LanPending
    };

    public string LatencyDisplay => LatencyMs.HasValue ? $"{LatencyMs} مللي ثانية" : "—";

    public override Task InitializeAsync()
    {
        IsBranchClient = _systemProfile.IsBranchClient;
        IsMainServer = _systemProfile.IsMainServer;
        ShowMainServerSettings = IsMainServer || _systemProfile.IsStandalone;
        DeploymentModeText = _systemProfile.DeploymentMode switch
        {
            DeploymentMode.MainServer => "حاسبة رئيسية",
            DeploymentMode.BranchClient => "حاسبة فرعية",
            _ => "حاسبة مستقلة"
        };

        BranchDisplayName = _systemProfile.Current.BranchDisplayName ?? string.Empty;

        if (IsBranchClient && _networkConnectionService.Current is { } profile)
        {
            MainServerHost = profile.MainServerHost;
            SqlPort = profile.SqlPort;
            SqlInstance = profile.SqlInstance ?? "SQLEXPRESS";
            SqlUsername = profile.SqlUsername;
            PairingCode = profile.PairingCode;
            SqlPassword = DpapiSecretProtector.Unprotect(profile.SqlPasswordEncrypted);
        }

        if (IsMainServer || _systemProfile.IsStandalone)
        {
            var settings = _mainServerHostingService.Current;
            ServerLabel = settings.ServerLabel;
            MainPairingCode = settings.PairingCode;
            AllowBranchConnections = settings.AllowBranchConnections;
            DiscoveryEnabled = settings.DiscoveryEnabled;
            SqlUsername = settings.BranchSqlUsername;
            SqlPassword = DpapiSecretProtector.Unprotect(settings.BranchSqlPasswordEncrypted);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DiscoverServersAsync()
    {
        IsSearching = true;
        StatusMessage = "جاري البحث على الشبكة...";
        DiscoveredServers.Clear();

        try
        {
            await using var client = new BranchServerDiscoveryClient();
            var servers = await client.DiscoverAsync(_systemProfile.ActiveSystem);
            foreach (var server in servers)
                DiscoveredServers.Add(server);

            StatusMessage = servers.Count == 0
                ? "لم يتم العثور على حاسبات رئيسية."
                : $"تم العثور على {servers.Count} جهاز.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"فشل البحث: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    partial void OnSelectedDiscoveredServerChanged(DiscoveredMainServer? value)
    {
        if (value is null)
            return;

        MainServerHost = value.Host;
        SqlPort = value.SqlPort;
        SqlInstance = value.SqlInstance ?? "SQLEXPRESS";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (!IsBranchClient)
            return;

        var profile = BuildBranchProfile();
        var result = await _networkConnectionService.TestConnectionAsync(profile, SqlPassword);

        IsConnectionOk = result.Success;
        ConnectionStatusText = result.Success ? "متصل" : "غير متصل";
        LatencyMs = result.LatencyMs;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task SaveBranchSettingsAsync()
    {
        if (!IsBranchClient)
            return;

        var profile = BuildBranchProfile();
        profile.SqlPasswordEncrypted = DpapiSecretProtector.Protect(SqlPassword);
        profile.LastSuccessfulConnection = DateTime.UtcNow;

        var test = await _networkConnectionService.TestConnectionAsync(profile, SqlPassword);
        if (!test.Success)
        {
            StatusMessage = test.Message;
            IsConnectionOk = false;
            ConnectionStatusText = "غير متصل";
            return;
        }

        _networkConnectionService.SaveBranchProfile(profile);
        _systemProfile.UpdateDeploymentMode(DeploymentMode.BranchClient, BranchDisplayName);
        IsConnectionOk = true;
        ConnectionStatusText = "متصل";
        LatencyMs = test.LatencyMs;
        StatusMessage = "تم حفظ إعدادات الربط بنجاح. أعد تشغيل التطبيق لتطبيق الاتصال.";
    }

    [RelayCommand]
    private async Task SaveMainServerSettingsAsync()
    {
        if (!IsAdmin)
            return;

        var settings = _mainServerHostingService.Current;
        settings.ServerLabel = ServerLabel;
        settings.PairingCode = string.IsNullOrWhiteSpace(MainPairingCode)
            ? _mainServerHostingService.GeneratePairingCode()
            : MainPairingCode;
        settings.AllowBranchConnections = AllowBranchConnections;
        settings.DiscoveryEnabled = DiscoveryEnabled;
        settings.BranchSqlUsername = SqlUsername;
        if (!string.IsNullOrWhiteSpace(SqlPassword))
            settings.BranchSqlPasswordEncrypted = DpapiSecretProtector.Protect(SqlPassword);

        _mainServerHostingService.SaveSettings(settings);
        MainPairingCode = settings.PairingCode;

        if (AllowBranchConnections)
        {
            if (!_mainServerHostingService.Current.SqlExpressConfigured)
            {
                var setup = await _mainServerHostingService.ConfigureSqlExpressAsync(_systemProfile.ActiveSystem);
                if (!setup.Success)
                {
                    StatusMessage = setup.Message;
                    return;
                }
            }

            await _mainServerHostingService.StartDiscoveryResponderAsync(
                _systemProfile.ActiveSystem,
                _systemProfile.ActiveDatabaseName);
        }
        else
        {
            await _mainServerHostingService.StopDiscoveryResponderAsync();
        }

        if (_systemProfile.IsStandalone && AllowBranchConnections)
            _systemProfile.UpdateDeploymentMode(DeploymentMode.MainServer);

        StatusMessage = "تم حفظ إعدادات الحاسبة الرئيسية.";
    }

    [RelayCommand]
    private void RegeneratePairingCode()
    {
        MainPairingCode = _mainServerHostingService.GeneratePairingCode();
    }

    private NetworkConnectionProfile BuildBranchProfile()
    {
        var profile = _networkConnectionService.CreateProfileForSystem(
            _systemProfile.ActiveSystem,
            _systemProfile.ActiveDatabaseName);

        profile.MainServerHost = MainServerHost.Trim();
        profile.SqlPort = SqlPort;
        profile.SqlInstance = SqlInstance;
        profile.SqlUsername = SqlUsername.Trim();
        profile.PairingCode = PairingCode.Trim();
        profile.ServerLabel = SelectedDiscoveredServer?.ServerLabel;
        profile.UseDiscovery = SelectedDiscoveredServer is not null;
        return profile;
    }
}
