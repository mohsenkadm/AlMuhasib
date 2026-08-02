using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSettingsViewModel : ViewModelBase
{
    private readonly IGoldSettingsService _settingsService;
    private readonly IGoldScaleService _scaleService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private decimal _mithqalGrams = 5;
    [ObservableProperty] private string _scaleComPort = string.Empty;
    [ObservableProperty] private int _scaleBaudRate = 9600;
    [ObservableProperty] private decimal _scaleStabilityThresholdGrams = 0.01m;
    [ObservableProperty] private bool _allowManualWeightEdit = true;
    [ObservableProperty] private decimal _lowStockAlertGrams = 10;
    [ObservableProperty] private int _overdueDaysThreshold = 30;
    [ObservableProperty] private string _enabledKaratsCsv = "24,22,21,18";
    [ObservableProperty] private GoldMakingChargeMode _defaultMakingChargeMode = GoldMakingChargeMode.Fixed;
    [ObservableProperty] private string _scaleStatusText = "غير متصل";
    [ObservableProperty] private bool _isScaleConnected;

    public ObservableCollection<string> AvailablePorts { get; } = [];
    public int[] BaudRates { get; } = [2400, 4800, 9600, 19200, 38400, 57600, 115200];

    public IReadOnlyList<GoldMakingChargeModeOption> MakingChargeModes { get; } =
    [
        new(GoldMakingChargeMode.Fixed, "مبلغ ثابت"),
        new(GoldMakingChargeMode.PerGram, "لكل غرام"),
        new(GoldMakingChargeMode.PercentOfGold, "نسبة من قيمة الذهب")
    ];

    public GoldSettingsViewModel(
        IGoldSettingsService settingsService,
        IGoldScaleService scaleService,
        ICurrentUserService currentUserService)
    {
        _settingsService = settingsService;
        _scaleService = scaleService;
        _currentUserService = currentUserService;
        PageTitle = "إعدادات الذهب";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Settings);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _settingsService.EnsureDefaultsAsync();
            var settings = await _settingsService.GetSettingsAsync();
            ApplySettings(settings);
            await RefreshPortsAsync();
            UpdateScaleStatus();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الإعدادات:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySettings(GoldSettings settings)
    {
        MithqalGrams = settings.MithqalGrams;
        ScaleComPort = settings.ScaleComPort;
        ScaleBaudRate = settings.ScaleBaudRate <= 0 ? 9600 : settings.ScaleBaudRate;
        ScaleStabilityThresholdGrams = settings.ScaleStabilityThresholdGrams;
        AllowManualWeightEdit = settings.AllowManualWeightEdit;
        LowStockAlertGrams = settings.LowStockAlertGrams;
        OverdueDaysThreshold = settings.OverdueDaysThreshold;
        EnabledKaratsCsv = settings.EnabledKaratsCsv;
        DefaultMakingChargeMode = settings.DefaultMakingChargeMode;
    }

    private void UpdateScaleStatus()
    {
        IsScaleConnected = _scaleService.IsConnected;
        ScaleStatusText = _scaleService.IsConnected
            ? $"متصل — {_scaleService.ConnectedPort}"
            : "غير متصل";
    }

    [RelayCommand]
    private async Task RefreshPortsAsync()
    {
        try
        {
            AvailablePorts.Clear();
            foreach (var port in await _scaleService.GetAvailablePortsAsync())
                AvailablePorts.Add(port);

            if (!string.IsNullOrWhiteSpace(ScaleComPort) && !AvailablePorts.Contains(ScaleComPort))
                AvailablePorts.Insert(0, ScaleComPort);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowWarning($"تعذر قراءة المنافذ: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل الإعدادات");
            return;
        }

        if (MithqalGrams <= 0)
        {
            BeautifulMessageDialog.ShowWarning("وزن المثقال يجب أن يكون أكبر من صفر");
            return;
        }

        try
        {
            IsBusy = true;
            var settings = await _settingsService.GetSettingsAsync();
            settings.MithqalGrams = MithqalGrams;
            settings.ScaleComPort = ScaleComPort?.Trim() ?? string.Empty;
            settings.ScaleBaudRate = ScaleBaudRate;
            settings.ScaleStabilityThresholdGrams = ScaleStabilityThresholdGrams;
            settings.AllowManualWeightEdit = AllowManualWeightEdit;
            settings.LowStockAlertGrams = LowStockAlertGrams;
            settings.OverdueDaysThreshold = OverdueDaysThreshold;
            settings.EnabledKaratsCsv = EnabledKaratsCsv?.Trim() ?? "24,22,21,18";
            settings.DefaultMakingChargeMode = DefaultMakingChargeMode;
            settings.UpdatedBy = _currentUserService.Username;

            await _settingsService.SaveSettingsAsync(settings);
            BeautifulMessageDialog.ShowSuccess("تم حفظ إعدادات الذهب");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConnectScaleAsync()
    {
        try
        {
            IsBusy = true;
            await _scaleService.ConnectAsync(
                string.IsNullOrWhiteSpace(ScaleComPort) ? null : ScaleComPort,
                ScaleBaudRate);
            UpdateScaleStatus();
            BeautifulMessageDialog.ShowSuccess("تم الاتصال بالميزان");
        }
        catch (Exception ex)
        {
            UpdateScaleStatus();
            BeautifulMessageDialog.ShowError($"فشل الاتصال بالميزان:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsBusy = true;
            await _scaleService.ConnectAsync(
                string.IsNullOrWhiteSpace(ScaleComPort) ? null : ScaleComPort,
                ScaleBaudRate);
            var grams = await _scaleService.ReadWeightGramsAsync();
            UpdateScaleStatus();
            BeautifulMessageDialog.ShowSuccess(
                $"اختبار الاتصال ناجح\nالمنفذ: {_scaleService.ConnectedPort}\nالوزن الحالي: {grams:N3} غرام");
        }
        catch (Exception ex)
        {
            UpdateScaleStatus();
            BeautifulMessageDialog.ShowError($"فشل اختبار الاتصال:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectScaleAsync()
    {
        try
        {
            await _scaleService.DisconnectAsync();
            UpdateScaleStatus();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }
}
