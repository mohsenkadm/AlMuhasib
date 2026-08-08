using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>تحصيل استلمه المندوب من عميل</summary>
public class SalesRepCollection : BaseEntity
{
    public int SalesRepresentativeId { get; set; }
    public int CustomerId { get; set; }

    public decimal Amount { get; set; }
    public DateTime CollectionDate { get; set; } = DateTime.Today;

    /// <summary>رقم الوصل</summary>
    public string? ReceiptNumber { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>المبلغ الذي سلّمه المندوب للصندوق/الشركة</summary>
    public decimal HandedOverAmount { get; set; }

    public DateTime? HandedOverAt { get; set; }

    public string? Notes { get; set; }

    /// <summary>ربط اختياري بفاتورة آجلة</summary>
    public int? InvoiceId { get; set; }

    public SalesRepresentative SalesRepresentative { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Invoice? Invoice { get; set; }

    public decimal PendingHandoverAmount => Math.Max(0, Amount - HandedOverAmount);
}
