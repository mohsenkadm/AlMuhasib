using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>عملية صيرفة: تحويل مبلغ بين قاصتين بعملتين مختلفتين (دولار ↔ دينار).</summary>
public class CurrencyExchange : BranchScopedEntity
{
    public int FromCashBoxId { get; set; }
    public int ToCashBoxId { get; set; }

    public AccountingCurrency FromCurrency { get; set; }
    public AccountingCurrency ToCurrency { get; set; }

    /// <summary>المبلغ المخصوم من قاصة المصدر (بعملتها).</summary>
    public decimal FromAmount { get; set; }

    /// <summary>المبلغ المضاف إلى قاصة الوجهة (بعملتها).</summary>
    public decimal ToAmount { get; set; }

    /// <summary>سعر الصرف المستخدم (دينار لكل دولار).</summary>
    public decimal FxRate { get; set; }

    public DateTime Date { get; set; }
    public string? Notes { get; set; }

    public CashBox FromCashBox { get; set; } = null!;
    public CashBox ToCashBox { get; set; } = null!;
}
