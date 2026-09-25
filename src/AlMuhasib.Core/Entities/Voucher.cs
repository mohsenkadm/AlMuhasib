using System.ComponentModel.DataAnnotations.Schema;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>السندات</summary>
public class Voucher : BaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }

    /// <summary>عملة السند — يجب أن تطابق عملة القاصة/البنك.</summary>
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;

    /// <summary>سعر الصرف وقت الحفظ (دولار→دينار).</summary>
    public decimal FxRate { get; set; } = 1m;

    public decimal Amount { get; set; }
    public decimal BankFees { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? InvestorId { get; set; }
    public int CashBoxId { get; set; }
    public int? BankAccountId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }

    /// <summary>ربط السند بفاتورة آجلة محددة (اختياري).</summary>
    public int? InvoiceId { get; set; }

    /// <summary>ربط السند بقسط محدد (اختياري).</summary>
    public int? InstallmentId { get; set; }

    /// <summary>تمت مطابقة الحركة مع كشف البنك.</summary>
    public bool IsReconciled { get; set; }

    public DateTime? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
    public Investor? Investor { get; set; }
    public CashBox CashBox { get; set; } = null!;
    public BankAccount? BankAccount { get; set; }
    public Invoice? Invoice { get; set; }
    public Installment? Installment { get; set; }

    [NotMapped]
    public string PartyDisplayName =>
        Customer?.Name ?? Supplier?.Name ?? Investor?.Name ?? string.Empty;
}
