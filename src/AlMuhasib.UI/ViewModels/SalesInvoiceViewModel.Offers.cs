using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesInvoiceViewModel
{
    private IProductOfferService? _productOfferService;
    private bool _isApplyingOffers;

    [ObservableProperty] private bool _showProductOffers;

    public void ConfigureProductOfferService(IProductOfferService productOfferService)
    {
        _productOfferService = productOfferService;
    }

    private void RefreshProductOffersFeatureVisibility()
    {
        ShowProductOffers = !IsReturnMode && !IsDamageMode && _featureFlags?.ProductOffers == true;
        if (!ShowProductOffers)
            RemoveOfferGiftRows();
    }

    private void RemoveOfferGiftRows()
    {
        var gifts = Items.Where(r => r.IsOfferGift).ToList();
        foreach (var row in gifts)
        {
            UnwireItemRow(row);
            Items.Remove(row);
        }
    }

    private async Task RefreshOfferGiftsAsync()
    {
        if (_isApplyingOffers || IsReturnMode || IsDamageMode)
            return;

        if (!ShowProductOffers || _productOfferService is null)
        {
            RemoveOfferGiftRows();
            return;
        }

        try
        {
            _isApplyingOffers = true;

            var triggerLines = Items
                .Where(r => !r.IsOfferGift && r.ProductId is > 0)
                .Select(r => (ProductId: r.ProductId!.Value, Quantity: r.Quantity))
                .ToList();

            if (triggerLines.Count == 0)
            {
                OfferApplicationHelper.ApplyToInvoiceRows(Items, [], WireItemRow, UnwireItemRow);
                return;
            }

            var triggerIds = triggerLines.Select(x => x.ProductId).Distinct().ToList();
            var offers = await _productOfferService.GetActiveOffersForTriggerProductsAsync(triggerIds);
            var gifts = OfferApplicationHelper.BuildGiftLines(triggerLines, offers);
            OfferApplicationHelper.ApplyToInvoiceRows(Items, gifts, WireItemRow, UnwireItemRow);
        }
        finally
        {
            _isApplyingOffers = false;
        }
    }
}
