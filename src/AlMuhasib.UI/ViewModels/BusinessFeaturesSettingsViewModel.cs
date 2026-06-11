using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class BusinessFeaturesSettingsViewModel : ViewModelBase
{
    private readonly IUserPreferencesService _preferences;
    private readonly IBackupService _backupService;

    [ObservableProperty] private bool _installmentRemindersEnabled = true;
    [ObservableProperty] private bool _reminderPlaySound = true;
    [ObservableProperty] private bool _reminderShowBanner = true;

    [ObservableProperty] private bool _autoBackupEnabled;
    [ObservableProperty] private BackupSchedule _backupSchedule = BackupSchedule.Daily;
    [ObservableProperty] private string? _backupFolderPath;
    [ObservableProperty] private int _backupRetainCount = 7;

    [ObservableProperty] private bool _purchaseReturns;
    [ObservableProperty] private bool _warehouseTransfers;
    [ObservableProperty] private bool _unitsOfMeasure;
    [ObservableProperty] private bool _expiryTracking;
    [ObservableProperty] private bool _serialNumbers;

    [ObservableProperty] private bool _templateMobileShop;
    [ObservableProperty] private bool _templateClothing;
    [ObservableProperty] private bool _templateConstruction;
    [ObservableProperty] private bool _templatePharmacy;

    [ObservableProperty] private int _idleLockMinutes;
    [ObservableProperty] private decimal _posMinInstallmentAmount = 50_000m;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _saveSuccessPulse;

    public int EnabledFeaturesCount =>
        CountEnabled(InstallmentRemindersEnabled, ReminderPlaySound, ReminderShowBanner,
            AutoBackupEnabled, PurchaseReturns, WarehouseTransfers, UnitsOfMeasure,
            ExpiryTracking, SerialNumbers, TemplateMobileShop, TemplateClothing,
            TemplateConstruction, TemplatePharmacy);

    public BusinessFeaturesSettingsViewModel(
        IUserPreferencesService preferences,
        IBackupService backupService,
        ICurrentUserService currentUserService)
    {
        _preferences = preferences;
        _backupService = backupService;
        PageTitle = "إعدادات الميزات";
        LoadPermissions(currentUserService, "BusinessFeatures");
    }

    public override Task InitializeAsync()
    {
        LoadFromPreferences();
        return Task.CompletedTask;
    }

    private void LoadFromPreferences()
    {
        var p = _preferences.Current;
        InstallmentRemindersEnabled = p.Reminders.InstallmentRemindersEnabled;
        ReminderPlaySound = p.Reminders.PlaySound;
        ReminderShowBanner = p.Reminders.ShowInAppBanner;

        AutoBackupEnabled = p.Backup.AutoBackupEnabled;
        BackupSchedule = p.Backup.Schedule;
        BackupFolderPath = p.Backup.BackupFolderPath ?? _backupService.GetDefaultBackupDirectory();
        BackupRetainCount = p.Backup.RetainCount;

        PurchaseReturns = p.FeatureFlags.PurchaseReturns;
        WarehouseTransfers = p.FeatureFlags.WarehouseTransfers;
        UnitsOfMeasure = p.FeatureFlags.UnitsOfMeasure;
        ExpiryTracking = p.FeatureFlags.ExpiryTracking;
        SerialNumbers = p.FeatureFlags.SerialNumbers;

        TemplateMobileShop = p.FeatureFlags.TemplateMobileShop;
        TemplateClothing = p.FeatureFlags.TemplateClothing;
        TemplateConstruction = p.FeatureFlags.TemplateConstruction;
        TemplatePharmacy = p.FeatureFlags.TemplatePharmacy;

        IdleLockMinutes = p.IdleLockMinutes;
        PosMinInstallmentAmount = p.PosMinInstallmentAmount;
        NotifyFeaturesCount();
    }

    private static int CountEnabled(params bool[] flags) => flags.Count(f => f);

    private void NotifyFeaturesCount() => OnPropertyChanged(nameof(EnabledFeaturesCount));

    partial void OnInstallmentRemindersEnabledChanged(bool value) => NotifyFeaturesCount();
    partial void OnReminderPlaySoundChanged(bool value) => NotifyFeaturesCount();
    partial void OnReminderShowBannerChanged(bool value) => NotifyFeaturesCount();
    partial void OnAutoBackupEnabledChanged(bool value) => NotifyFeaturesCount();
    partial void OnPurchaseReturnsChanged(bool value) => NotifyFeaturesCount();
    partial void OnWarehouseTransfersChanged(bool value) => NotifyFeaturesCount();
    partial void OnUnitsOfMeasureChanged(bool value) => NotifyFeaturesCount();
    partial void OnExpiryTrackingChanged(bool value) => NotifyFeaturesCount();
    partial void OnSerialNumbersChanged(bool value) => NotifyFeaturesCount();
    partial void OnTemplateMobileShopChanged(bool value) => NotifyFeaturesCount();
    partial void OnTemplateClothingChanged(bool value) => NotifyFeaturesCount();
    partial void OnTemplateConstructionChanged(bool value) => NotifyFeaturesCount();
    partial void OnTemplatePharmacyChanged(bool value) => NotifyFeaturesCount();

    [RelayCommand]
    private void BrowseBackupFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "اختر مجلد النسخ الاحتياطي",
            InitialDirectory = BackupFolderPath ?? _backupService.GetDefaultBackupDirectory()
        };
        if (dlg.ShowDialog() == true)
            BackupFolderPath = dlg.FolderName;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _preferences.Update(p =>
        {
            p.Reminders.InstallmentRemindersEnabled = InstallmentRemindersEnabled;
            p.Reminders.PlaySound = ReminderPlaySound;
            p.Reminders.ShowInAppBanner = ReminderShowBanner;

            p.Backup.AutoBackupEnabled = AutoBackupEnabled;
            p.Backup.Schedule = BackupSchedule;
            p.Backup.BackupFolderPath = BackupFolderPath;
            p.Backup.RetainCount = Math.Max(1, BackupRetainCount);

            p.FeatureFlags.PurchaseReturns = PurchaseReturns;
            p.FeatureFlags.WarehouseTransfers = WarehouseTransfers;
            p.FeatureFlags.UnitsOfMeasure = UnitsOfMeasure;
            p.FeatureFlags.ExpiryTracking = ExpiryTracking;
            p.FeatureFlags.SerialNumbers = SerialNumbers;

            p.FeatureFlags.TemplateMobileShop = TemplateMobileShop;
            p.FeatureFlags.TemplateClothing = TemplateClothing;
            p.FeatureFlags.TemplateConstruction = TemplateConstruction;
            p.FeatureFlags.TemplatePharmacy = TemplatePharmacy;

            p.IdleLockMinutes = Math.Max(0, IdleLockMinutes);
            p.PosMinInstallmentAmount = Math.Max(0, PosMinInstallmentAmount);
        });

        StatusMessage = "تم حفظ الإعدادات بنجاح";
        SaveSuccessPulse = true;
        BeautifulMessageDialog.ShowSuccess("تم حفظ إعدادات الميزات");
        await Task.Delay(1200);
        SaveSuccessPulse = false;
    }
}
