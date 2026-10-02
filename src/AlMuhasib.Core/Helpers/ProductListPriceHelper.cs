using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>اختيار سعر القائمة حسب عملة المستند — بدون تحويل عبر سعر الصرف.</summary>
public static class ProductListPriceHelper
{
    /// <summary>
    /// يعيد سعر البيع أو الشراء بالعملة المطلوبة.
    /// للدولار: إن كان السعر غير معرّف (≤ 0) يُعاد 0 — لا يُحوَّل من الدينار.
    /// </summary>
    public static decimal ResolveListPrice(
        ProductPrice? price,
        AccountingCurrency currency,
        bool isPurchase = false)
    {
        if (price is null)
            return 0m;

        if (currency == AccountingCurrency.USD)
        {
            var usd = isPurchase ? price.PurchasePriceUsd : price.SalePriceUsd;
            return usd > 0 ? AccountingCurrencyHelper.RoundUsd(usd) : 0m;
        }

        var iqd = isPurchase ? price.PurchasePrice : price.SalePrice;
        return iqd > 0 ? AccountingCurrencyHelper.RoundIqd(iqd) : 0m;
    }

    public static decimal ResolveListPrice(
        decimal iqdPrice,
        decimal usdPrice,
        AccountingCurrency currency)
    {
        if (currency == AccountingCurrency.USD)
            return usdPrice > 0 ? AccountingCurrencyHelper.RoundUsd(usdPrice) : 0m;

        return iqdPrice > 0 ? AccountingCurrencyHelper.RoundIqd(iqdPrice) : 0m;
    }
}
