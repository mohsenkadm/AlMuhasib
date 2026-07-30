using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core;

/// <summary>حساب وعرض خصم المنتج والفاتورة.</summary>
public static class ProductDiscountHelper
{
    public static bool IsDiscountActive(Product? product, DateTime? asOfUtc = null)
    {
        if (product is null || product.DiscountType == DiscountType.None || product.DiscountValue <= 0)
            return false;

        var now = asOfUtc ?? DateTime.UtcNow;
        if (product.DiscountExpiresAt is DateTime expires && expires < now)
            return false;

        return true;
    }

    /// <summary>مبلغ خصم السطر من إعدادات المنتج (0 إن لم يكن فعّالاً).</summary>
    public static decimal CalculateLineDiscount(Product? product, decimal quantity, decimal unitPrice, DateTime? asOfUtc = null)
    {
        if (!IsDiscountActive(product, asOfUtc) || product is null)
            return 0m;

        var qty = Math.Abs(quantity);
        var gross = qty * unitPrice;
        if (gross <= 0)
            return 0m;

        decimal discount = product.DiscountType switch
        {
            DiscountType.Percentage => gross * product.DiscountValue / 100m,
            DiscountType.FixedAmount => product.DiscountValue * qty,
            _ => 0m
        };

        if (discount < 0) discount = 0m;
        if (discount > gross) discount = gross;
        return Math.Round(discount, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateLineTotal(decimal quantity, decimal unitPrice, decimal discountAmount)
    {
        var gross = quantity * unitPrice;
        var total = gross - Math.Abs(discountAmount);
        // Preserve sign of quantity for returns-style negative lines
        if (gross >= 0)
            return total < 0 ? 0m : total;
        return total > 0 ? 0m : total;
    }

    /// <summary>حساب خصم الفاتورة الكلي من نسبة أو قيمة.</summary>
    public static decimal CalculateInvoiceDiscount(DiscountType type, decimal value, decimal subtotal)
    {
        if (type == DiscountType.None || value <= 0 || subtotal <= 0)
            return 0m;

        decimal discount = type switch
        {
            DiscountType.Percentage => subtotal * value / 100m,
            DiscountType.FixedAmount => value,
            _ => 0m
        };

        if (discount < 0) discount = 0m;
        if (discount > subtotal) discount = subtotal;
        return Math.Round(discount, 2, MidpointRounding.AwayFromZero);
    }

    public static string FormatDiscountDisplay(Product product)
    {
        if (product.DiscountType == DiscountType.None || product.DiscountValue <= 0)
            return "—";

        var text = product.DiscountType switch
        {
            DiscountType.Percentage => $"{product.DiscountValue:0.##}%",
            DiscountType.FixedAmount => $"{product.DiscountValue:N0} د.ع",
            _ => "—"
        };

        if (product.DiscountExpiresAt is DateTime expires)
        {
            var local = expires.Kind == DateTimeKind.Utc ? expires.ToLocalTime() : expires;
            text += $" (حتى {local:yyyy/MM/dd})";
            if (expires < DateTime.UtcNow)
                text += " — منتهٍ";
        }

        return text;
    }

    public static string FormatDiscountTypeLabel(DiscountType type) => type switch
    {
        DiscountType.Percentage => "نسبة",
        DiscountType.FixedAmount => "قيمة",
        _ => "بدون"
    };
}
