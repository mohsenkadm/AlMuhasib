using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesInvoiceViewModel
{
    private ILoyaltyService? _loyaltyService;
    private bool _loyaltyQuoteBusy;

    [ObservableProperty] private bool _showLoyaltyPanel;
    [ObservableProperty] private int _loyaltyBalance;
    [ObservableProperty] private int _loyaltyExpectedEarnPoints;
    [ObservableProperty] private int _loyaltyMaxRedeemPoints;
    [ObservableProperty] private int _loyaltyRedeemPoints;
    [ObservableProperty] private decimal _loyaltyDiscountAmount;
    [ObservableProperty] private string _loyaltyStatusMessage = string.Empty;
    [ObservableProperty] private string _loyaltyCustomerLabel = "اختر زبوناً لتفعيل الولاء";
    [ObservableProperty] private bool _hasLoyaltyCustomer;
    [ObservableProperty] private bool _hasLoyaltyStatusMessage;

    private void ConfigureLoyaltyService(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    partial void OnLoyaltyRedeemPointsChanged(int value)
    {
        if (_loyaltyQuoteBusy) return;
        _ = RefreshLoyaltyQuoteAsync();
    }

    [RelayCommand]
    private void RedeemMaxLoyalty()
    {
        if (LoyaltyMaxRedeemPoints <= 0) return;
        LoyaltyRedeemPoints = LoyaltyMaxRedeemPoints;
    }

    [RelayCommand]
    private void ClearLoyaltyRedeem()
    {
        LoyaltyRedeemPoints = 0;
        LoyaltyDiscountAmount = 0m;
        LoyaltyStatusMessage = string.Empty;
        HasLoyaltyStatusMessage = false;
        RecalculateTotals();
    }

    private async Task RefreshLoyaltyQuoteAsync()
    {
        if (!ShowLoyaltyPanel || _loyaltyService is null || SelectedCustomer is null)
        {
            ResetLoyaltyUi(keepPanel: ShowLoyaltyPanel);
            RecalculateTotals();
            return;
        }

        try
        {
            _loyaltyQuoteBusy = true;
            HasLoyaltyCustomer = true;
            LoyaltyCustomerLabel = SelectedCustomer.Name;

            var baseAmount = Math.Max(0m, Subtotal - (ShowProductDiscount ? InvoiceDiscountAmount : 0m));
            var quote = await _loyaltyService.QuoteAsync(
                SelectedCustomer.Id,
                baseAmount,
                LoyaltyRedeemPoints,
                SelectedPaymentMethod);

            LoyaltyBalance = quote.Balance;
            LoyaltyExpectedEarnPoints = quote.ExpectedEarnPoints;
            LoyaltyMaxRedeemPoints = quote.MaxRedeemablePoints;
            LoyaltyDiscountAmount = quote.RedeemDiscount;
            LoyaltyStatusMessage = quote.Error ?? string.Empty;
            HasLoyaltyStatusMessage = !string.IsNullOrWhiteSpace(LoyaltyStatusMessage);

            if (LoyaltyRedeemPoints != quote.RequestedRedeemPoints && !string.IsNullOrEmpty(quote.Error))
            {
                // أبقِ الإدخال لكن اصفّر الخصم عند الخطأ
                LoyaltyDiscountAmount = 0m;
            }
        }
        catch (Exception ex)
        {
            LoyaltyStatusMessage = ex.Message;
            HasLoyaltyStatusMessage = true;
            LoyaltyDiscountAmount = 0m;
        }
        finally
        {
            _loyaltyQuoteBusy = false;
            RecalculateTotals();
        }
    }

    private void ResetLoyaltyUi(bool keepPanel)
    {
        LoyaltyBalance = 0;
        LoyaltyExpectedEarnPoints = 0;
        LoyaltyMaxRedeemPoints = 0;
        LoyaltyDiscountAmount = 0m;
        if (!keepPanel || SelectedCustomer is null)
        {
            LoyaltyRedeemPoints = 0;
            LoyaltyCustomerLabel = "اختر زبوناً لتفعيل الولاء";
            HasLoyaltyCustomer = false;
        }

        LoyaltyStatusMessage = string.Empty;
        HasLoyaltyStatusMessage = false;
    }

    private void RefreshLoyaltyFeatureVisibility()
    {
        var enabled = _featureFlags?.LoyaltySystem == true;
        ShowLoyaltyPanel = enabled;
        if (!enabled)
        {
            ResetLoyaltyUi(keepPanel: false);
            LoyaltyRedeemPoints = 0;
        }
    }
}
