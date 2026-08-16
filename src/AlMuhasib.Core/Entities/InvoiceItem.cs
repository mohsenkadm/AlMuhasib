namespace AlMuhasib.Core.Entities;

/// <summary>تفاصيل الفاتورة</summary>
public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public int? PricingTypeId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    /// <summary>مبلغ خصم السطر (من خصم المنتج أو يدوي).</summary>
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }

    /// <summary>سطر هدية من عرض منتجات (سعر صفر).</summary>
    public bool IsOfferGift { get; set; }

    /// <summary>معرّف العرض الذي أنتج سطر الهدية.</summary>
    public int? OfferId { get; set; }

    /// <summary>حقول مخصصة (IMEI، مقاس، لون...) JSON.</summary>
    public string? CustomFieldsJson { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
    public Product? Product { get; set; }
    public PricingType? PricingType { get; set; }
}
