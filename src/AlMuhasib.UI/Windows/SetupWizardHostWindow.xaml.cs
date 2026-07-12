using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure;
using AlMuhasib.Infrastructure.Network;
using AlMuhasib.Infrastructure.Security;
using AlMuhasib.Infrastructure.Services;

namespace AlMuhasib.UI.Windows;

public partial class SetupWizardHostWindow : Window
{
    private readonly NetworkConnectionService _networkConnectionService = new();
    private readonly MainServerHostingService _mainServerHostingService;
    private int _currentStep = 1;
    private bool _mainServerConfigured;
    private bool _branchConnectionTested;

    public ApplicationSystemType? SelectedSystem { get; private set; }
    public DeploymentMode SelectedDeploymentMode { get; private set; } = DeploymentMode.Standalone;
    public NetworkConnectionProfile? SavedBranchProfile { get; private set; }
    public MainServerSettings? SavedMainServerSettings { get; private set; }
    public string? BranchDisplayName { get; private set; }

    public SetupWizardHostWindow()
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        _mainServerHostingService = new MainServerHostingService(configuration);
        InitializeComponent();

        ServerLabelText.Text = _mainServerHostingService.Current.ServerLabel;
        MainPairingCodeText.Text = _mainServerHostingService.Current.PairingCode;
        SqlUsernameText.Text = _mainServerHostingService.Current.BranchSqlUsername;
    }

    private void OnAccountingSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.Accounting, AccountingCard, "#1565C0", "#E3F2FD");

    private void OnCarSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.CarContracts, CarCard, "#2E7D32", "#E8F5E9");

    private void OnCarTradeSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.CarTrading, CarTradeCard, "#E65100", "#FFF3E0");

    private void OnHotelSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.HotelManagement, HotelCard, "#6A1B9A", "#F3E5F5");

    private void SelectSystem(ApplicationSystemType system, Border card, string accent, string bg)
    {
        SelectedSystem = system;
        ContinueButton.IsEnabled = true;

        ResetCard(AccountingCard);
        ResetCard(CarCard);
        ResetCard(CarTradeCard);
        ResetCard(HotelCard);

        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent)!);
        card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)!);
    }

    private void OnMainServerSelected(object sender, MouseButtonEventArgs e) =>
        SelectDeployment(DeploymentMode.MainServer, MainServerCard, "#2E7D32", "#E8F5E9");

    private void OnBranchClientSelected(object sender, MouseButtonEventArgs e) =>
        SelectDeployment(DeploymentMode.BranchClient, BranchClientCard, "#1565C0", "#E3F2FD");

    private void OnStandaloneSelected(object sender, MouseButtonEventArgs e) =>
        SelectDeployment(DeploymentMode.Standalone, StandaloneCard, "#E65100", "#FFF3E0");

    private void SelectDeployment(DeploymentMode mode, Border card, string accent, string bg)
    {
        SelectedDeploymentMode = mode;
        ContinueButton.IsEnabled = true;

        ResetCard(MainServerCard);
        ResetCard(BranchClientCard);
        ResetCard(StandaloneCard);

        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent)!);
        card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)!);
    }

    private static void ResetCard(Border card)
    {
        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")!);
        card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFAFA")!);
    }

    private async void OnDiscoverClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSystem is null)
            return;

        DiscoverButton.IsEnabled = false;
        DiscoveryStatusText.Text = "جاري البحث على الشبكة...";
        DiscoveredServersList.Items.Clear();

        try
        {
            await using var client = new BranchServerDiscoveryClient();
            var servers = await client.DiscoverAsync(SelectedSystem.Value);

            if (servers.Count == 0)
            {
                DiscoveryStatusText.Text = "لم يتم العثور على حاسبات رئيسية. أدخل عنوان IP يدوياً.";
                return;
            }

            foreach (var server in servers)
                DiscoveredServersList.Items.Add(server);

            DiscoveryStatusText.Text = $"تم العثور على {servers.Count} جهاز.";
        }
        catch (Exception ex)
        {
            DiscoveryStatusText.Text = $"فشل البحث: {ex.Message}";
        }
        finally
        {
            DiscoverButton.IsEnabled = true;
        }
    }

    private void OnDiscoveredServerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (DiscoveredServersList.SelectedItem is not DiscoveredMainServer server)
            return;

        MainServerHostText.Text = server.Host;
        if (!string.IsNullOrWhiteSpace(server.SqlInstance))
            SqlUsernameText.Text = _mainServerHostingService.Current.BranchSqlUsername;
    }

    private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSystem is null)
            return;

        var profile = BuildBranchProfileFromUi();
        var password = SqlPasswordBox.Password;

        TestConnectionButton.IsEnabled = false;
        ConnectionTestResultText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#607D8B")!);
        ConnectionTestResultText.Text = "جاري اختبار الاتصال...";

        var result = await _networkConnectionService.TestConnectionAsync(profile, password);
        ConnectionTestResultText.Text = result.Message;
        ConnectionTestResultText.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(result.Success ? "#2E7D32" : "#C62828")!);

        _branchConnectionTested = result.Success;
        ContinueButton.IsEnabled = result.Success;
        TestConnectionButton.IsEnabled = true;
    }

    private async void OnConfigureMainServerClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSystem is null)
            return;

        ConfigureMainServerButton.IsEnabled = false;
        MainSetupStatusText.Text = "جاري تهيئة SQL Express...";

        var settings = _mainServerHostingService.Current;
        settings.AllowBranchConnections = true;
        settings.DiscoveryEnabled = true;
        settings.ServerLabel = string.IsNullOrWhiteSpace(ServerLabelText.Text)
            ? settings.ServerLabel
            : ServerLabelText.Text.Trim();
        settings.PairingCode = MainPairingCodeText.Text.Trim();
        _mainServerHostingService.SaveSettings(settings);

        var result = await _mainServerHostingService.ConfigureSqlExpressAsync(SelectedSystem.Value);
        MainSetupStatusText.Text = result.Message;
        MainSetupStatusText.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(result.Success ? "#2E7D32" : "#C62828")!);

        _mainServerConfigured = result.Success;
        ContinueButton.IsEnabled = result.Success;
        ConfigureMainServerButton.IsEnabled = true;

        if (result.Success)
            SavedMainServerSettings = _mainServerHostingService.Current;
    }

    private NetworkConnectionProfile BuildBranchProfileFromUi()
    {
        var databaseName = SelectedSystem switch
        {
            ApplicationSystemType.CarContracts => SystemConnectionStrings.CarContractsDatabase,
            ApplicationSystemType.HotelManagement => SystemConnectionStrings.HotelsDatabase,
            ApplicationSystemType.CarTrading => SystemConnectionStrings.CarTradingDatabase,
            _ => SystemConnectionStrings.AccountingDatabase
        };

        return new NetworkConnectionProfile
        {
            MainServerHost = MainServerHostText.Text.Trim(),
            SqlPort = 1433,
            SqlInstance = "SQLEXPRESS",
            DatabaseName = databaseName,
            SystemType = SelectedSystem ?? ApplicationSystemType.Accounting,
            SqlUsername = SqlUsernameText.Text.Trim(),
            SqlPasswordEncrypted = string.IsNullOrWhiteSpace(SqlPasswordBox.Password)
                ? string.Empty
                : DpapiSecretProtector.Protect(SqlPasswordBox.Password),
            PairingCode = PairingCodeText.Text.Trim(),
            UseDiscovery = DiscoveredServersList.SelectedItem is not null,
            ConnectionTimeoutSeconds = 15,
            ServerLabel = (DiscoveredServersList.SelectedItem as DiscoveredMainServer)?.ServerLabel
        };
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_currentStep == 3)
        {
            ShowStep(2);
            return;
        }

        if (_currentStep == 2)
            ShowStep(1);
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        if (_currentStep == 1)
        {
            if (SelectedSystem is null)
                return;

            ShowStep(2);
            return;
        }

        if (_currentStep == 2)
        {
            if (SelectedDeploymentMode == DeploymentMode.BranchClient)
            {
                ShowStep(3, isBranch: true);
                ContinueButton.IsEnabled = _branchConnectionTested;
                return;
            }

            if (SelectedDeploymentMode == DeploymentMode.MainServer)
            {
                ShowStep(3, isBranch: false);
                ContinueButton.IsEnabled = _mainServerConfigured;
                return;
            }

            FinishWizard();
            return;
        }

        if (_currentStep == 3)
        {
            if (SelectedDeploymentMode == DeploymentMode.BranchClient)
            {
                var profile = BuildBranchProfileFromUi();
                if (!_branchConnectionTested)
                {
                    ConnectionTestResultText.Text = "يرجى اختبار الاتصال قبل المتابعة.";
                    return;
                }

                profile.LastSuccessfulConnection = DateTime.UtcNow;
                _networkConnectionService.SaveBranchProfile(profile);
                SavedBranchProfile = profile;
                BranchDisplayName = string.IsNullOrWhiteSpace(BranchNameText.Text) ? null : BranchNameText.Text.Trim();
            }

            FinishWizard();
        }
    }

    private void FinishWizard()
    {
        if (SelectedDeploymentMode == DeploymentMode.MainServer)
        {
            var settings = _mainServerHostingService.Current;
            settings.AllowBranchConnections = true;
            settings.DiscoveryEnabled = true;
            settings.ServerLabel = string.IsNullOrWhiteSpace(ServerLabelText.Text)
                ? settings.ServerLabel
                : ServerLabelText.Text.Trim();
            settings.PairingCode = MainPairingCodeText.Text.Trim();
            _mainServerHostingService.SaveSettings(settings);
            SavedMainServerSettings = settings;
        }

        DialogResult = true;
        Close();
    }

    private void ShowStep(int step, bool isBranch = false)
    {
        _currentStep = step;

        Step1Panel.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3BranchPanel.Visibility = step == 3 && isBranch ? Visibility.Visible : Visibility.Collapsed;
        Step3MainPanel.Visibility = step == 3 && !isBranch ? Visibility.Visible : Visibility.Collapsed;

        BackButton.Visibility = step > 1 ? Visibility.Visible : Visibility.Collapsed;
        Step3Connector.Visibility = step >= 3 ? Visibility.Visible : Visibility.Collapsed;
        Step3Indicator.Visibility = step >= 3 ? Visibility.Visible : Visibility.Collapsed;

        UpdateStepIndicators(step);

        StepSubtitle.Text = step switch
        {
            1 => "اختر نوع النظام المناسب لعملك",
            2 => "اختر نوع هذه الحاسبة في شبكتك",
            3 when isBranch => "اربط هذه الحاسبة بالحاسبة الرئيسية",
            3 => "هيّئ الحاسبة الرئيسية لاستقبال الفروع",
            _ => string.Empty
        };

        ContinueButton.Content = step == 3 ? "إنهاء الإعداد" : "متابعة";
        ContinueButton.IsEnabled = step switch
        {
            1 => SelectedSystem.HasValue,
            2 => true,
            3 when isBranch => _branchConnectionTested,
            3 => _mainServerConfigured,
            _ => false
        };
    }

    private void UpdateStepIndicators(int step)
    {
        SetIndicator(Step1Indicator, step >= 1, active: step == 1);
        SetIndicator(Step2Indicator, step >= 2, active: step == 2);
        SetIndicator(Step3Indicator, step >= 3, active: step == 3);
    }

    private static void SetIndicator(Border indicator, bool reached, bool active)
    {
        indicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
            active ? "#1565C0" : reached ? "#90CAF9" : "#E0E0E0")!);

        if (indicator.Child is TextBlock text)
            text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                reached ? "White" : "#78909C")!);
    }
}
