using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class PosQuickSaleViewModel
{
    private IProductOfferService? _productOfferService;
    private bool _isApplyingOffers;

    [ObservableProperty] private bool _showProductOffers;

    private void ConfigureProductOfferService(IProductOfferService productOfferService)
    {
        _productOfferService = productOfferService;
    }

    private void RefreshProductOffersFeatureVisibility()
    {
        ShowProductOffers = _featureFlags.ProductOffers;
        if (!ShowProductOffers)
            RemoveOfferGiftLines();
    }

    private void RemoveOfferGiftLines()
    {
        var gifts = CartLines.Where(l => l.IsOfferGift).ToList();
        foreach (var line in gifts)
            CartLines.Remove(line);
    }

    private async Task RefreshOfferGiftsAsync()
    {
        if (_isApplyingOffers)
            return;

        if (!ShowProductOffers || _productOfferService is null)
        {
            RemoveOfferGiftLines();
            return;
        }

        try
        {
            _isApplyingOffers = true;

            var triggerLines = CartLines
                .Where(l => !l.IsOfferGift && l.ProductId > 0)
                .Select(l => (ProductId: l.ProductId, Quantity: l.Quantity))
                .ToList();

            if (triggerLines.Count == 0)
            {
                OfferApplicationHelper.ApplyToPosCart(CartLines, []);
                return;
            }

            var triggerIds = triggerLines.Select(x => x.ProductId).Distinct().ToList();
            var offers = await _productOfferService.GetActiveOffersForTriggerProductsAsync(triggerIds);
            var gifts = OfferApplicationHelper.BuildGiftLines(triggerLines, offers);
            OfferApplicationHelper.ApplyToPosCart(CartLines, gifts);
        }
        finally
        {
            _isApplyingOffers = false;
        }
    }
}
