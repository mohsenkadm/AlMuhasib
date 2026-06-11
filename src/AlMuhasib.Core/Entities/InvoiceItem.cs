namespace AlMuhasib.Core.Entities;

/// <summary>تفاصيل الفاتورة</summary>
public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    /// <summary>حقول مخصصة (IMEI، مقاس، لون...) JSON.</summary>
    public string? CustomFieldsJson { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
    public Product? Product { get; set; }
}
