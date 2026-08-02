using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class LoyaltySettingsViewModel : ViewModelBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IFeatureFlagService _featureFlags;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private decimal _pointsPerAmount = 1000m;
    [ObservableProperty] private decimal _pointValueInCurrency = 100m;
    [ObservableProperty] private decimal _minInvoiceAmountToEarn;
    [ObservableProperty] private int _minPointsToRedeem = 1;
    [ObservableProperty] private decimal _maxRedeemPercentOfInvoice = 50m;
    [ObservableProperty] private int? _pointsExpireAfterDays;
    [ObservableProperty] private bool _earnOnCreditSales = true;
    [ObservableProperty] private bool _roundEarnDown = true;
    [ObservableProperty] private string _previewText = string.Empty;
    [ObservableProperty] private bool _featureEnabled;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public LoyaltySettingsViewModel(
        ILoyaltyService loyaltyService,
        IFeatureFlagService featureFlags,
        ICurrentUserService currentUser)
    {
        _loyaltyService = loyaltyService;
        _featureFlags = featureFlags;
        _currentUser = currentUser;
        PageTitle = "إعدادات الولاء";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUser, "LoyaltySettings");
        FeatureEnabled = _featureFlags.LoyaltySystem;
        _featureFlags.FlagsChanged += (_, _) => FeatureEnabled = _featureFlags.LoyaltySystem;

        try
        {
            IsBusy = true;
            var s = await _loyaltyService.GetOrCreateSettingsAsync();
            PointsPerAmount = s.PointsPerAmount;
            PointValueInCurrency = s.PointValueInCurrency;
            MinInvoiceAmountToEarn = s.MinInvoiceAmountToEarn;
            MinPointsToRedeem = s.MinPointsToRedeem;
            MaxRedeemPercentOfInvoice = s.MaxRedeemPercentOfInvoice;
            PointsExpireAfterDays = s.PointsExpireAfterDays;
            EarnOnCreditSales = s.EarnOnCreditSales;
            RoundEarnDown = s.RoundEarnDown;
            UpdatePreview();
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

    partial void OnPointsPerAmountChanged(decimal value) => UpdatePreview();
    partial void OnPointValueInCurrencyChanged(decimal value) => UpdatePreview();

    private void UpdatePreview()
    {
        var per = PointsPerAmount <= 0 ? 1000m : PointsPerAmount;
        var val = Math.Max(0m, PointValueInCurrency);
        PreviewText = $"كل {per:N0} د.ع → 1 نقطة    |    50 نقطة = {50m * val:N0} د.ع خصم";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            await _loyaltyService.SaveSettingsAsync(new LoyaltySettings
            {
                PointsPerAmount = PointsPerAmount,
                PointValueInCurrency = PointValueInCurrency,
                MinInvoiceAmountToEarn = MinInvoiceAmountToEarn,
                MinPointsToRedeem = MinPointsToRedeem,
                MaxRedeemPercentOfInvoice = MaxRedeemPercentOfInvoice,
                PointsExpireAfterDays = PointsExpireAfterDays,
                EarnOnCreditSales = EarnOnCreditSales,
                RoundEarnDown = RoundEarnDown
            });
            StatusMessage = "تم حفظ إعدادات الولاء";
            BeautifulMessageDialog.ShowSuccess("تم حفظ إعدادات الولاء");
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
}
