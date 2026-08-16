namespace AlMuhasib.Core.Entities;

/// <summary>عرض منتجات: اشترِ كمية من منتج واحصل على منتج آخر مجاناً.</summary>
public class ProductOffer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>المنتج الذي يفعّل العرض عند شرائه.</summary>
    public int TriggerProductId { get; set; }

    /// <summary>الكمية المطلوبة من المنتج المشغّل لكل دورة عرض.</summary>
    public decimal TriggerQuantity { get; set; }

    /// <summary>منتج الهدية.</summary>
    public int GiftProductId { get; set; }

    /// <summary>كمية الهدية لكل دورة عرض مكتملة.</summary>
    public decimal GiftQuantity { get; set; }

    public string? Notes { get; set; }

    public Product TriggerProduct { get; set; } = null!;
    public Product GiftProduct { get; set; } = null!;
}
