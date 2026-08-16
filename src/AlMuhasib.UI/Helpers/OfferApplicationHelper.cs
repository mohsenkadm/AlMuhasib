using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>تطبيق عروض المنتجات على بنود فاتورة البيع / سلة POS.</summary>
public static class OfferApplicationHelper
{
    public sealed record GiftLineSpec(
        int OfferId,
        string OfferName,
        int GiftProductId,
        string GiftProductName,
        Product GiftProduct,
        decimal Quantity);

    /// <summary>
    /// يجمع كميات المنتجات المشغّلة ويحسب هدايا العروض النشطة.
    /// </summary>
    public static IReadOnlyList<GiftLineSpec> BuildGiftLines(
        IEnumerable<(int ProductId, decimal Quantity)> soldLines,
        IEnumerable<ProductOffer> activeOffers)
    {
        var qtyByProduct = soldLines
            .Where(x => x.ProductId > 0 && x.Quantity > 0)
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        if (qtyByProduct.Count == 0)
            return [];

        var gifts = new List<GiftLineSpec>();
        foreach (var offer in activeOffers.Where(o => o.IsActive))
        {
            if (!qtyByProduct.TryGetValue(offer.TriggerProductId, out var soldQty))
                continue;

            var giftQty = ProductOfferCalculator.ComputeGiftQuantity(
                soldQty, offer.TriggerQuantity, offer.GiftQuantity);
            if (giftQty <= 0)
                continue;

            var giftProduct = offer.GiftProduct;
            if (giftProduct is null)
                continue;

            gifts.Add(new GiftLineSpec(
                offer.Id,
                offer.Name,
                offer.GiftProductId,
                giftProduct.Name,
                giftProduct,
                giftQty));
        }

        return gifts;
    }

    public static void ApplyToInvoiceRows(
        IList<InvoiceItemRow> items,
        IReadOnlyList<GiftLineSpec> gifts,
        Action<InvoiceItemRow>? wireRow = null,
        Action<InvoiceItemRow>? unwireRow = null)
    {
        // إزالة هدايا قديمة
        var oldGifts = items.Where(r => r.IsOfferGift).ToList();
        foreach (var row in oldGifts)
        {
            unwireRow?.Invoke(row);
            items.Remove(row);
        }

        foreach (var gift in gifts)
        {
            var row = new InvoiceItemRow
            {
                IsOfferGift = true,
                OfferId = gift.OfferId,
                ProductId = gift.GiftProductId,
                ItemName = gift.GiftProductName,
                Quantity = gift.Quantity,
                UnitPrice = 0m,
                DiscountAmount = 0m,
                TotalPrice = 0m,
                ProductDiscountFeatureEnabled = false
            };
            row.AttachProductSilent(gift.GiftProduct);
            wireRow?.Invoke(row);
            items.Add(row);
        }
    }

    public static void ApplyToPosCart(
        IList<PosCartLine> cart,
        IReadOnlyList<GiftLineSpec> gifts)
    {
        var oldGifts = cart.Where(l => l.IsOfferGift).ToList();
        foreach (var line in oldGifts)
            cart.Remove(line);

        foreach (var gift in gifts)
        {
            cart.Add(new PosCartLine
            {
                ProductId = gift.GiftProductId,
                ProductName = gift.GiftProductName,
                SourceProduct = gift.GiftProduct,
                Quantity = gift.Quantity,
                UnitPrice = 0m,
                DiscountAmount = 0m,
                LineTotal = 0m,
                IsOfferGift = true,
                OfferId = gift.OfferId,
                ProductDiscountFeatureEnabled = false
            });
        }
    }
}
