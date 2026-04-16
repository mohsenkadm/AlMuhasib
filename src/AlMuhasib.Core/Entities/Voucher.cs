using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>السندات</summary>
public class Voucher : BaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public decimal Amount { get; set; }
    public decimal BankFees { get; set; }
    public int? CustomerId { get; set; }
    public int? InvestorId { get; set; }
    public int CashBoxId { get; set; }
    public int? BankAccountId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
    public Investor? Investor { get; set; }
    public CashBox CashBox { get; set; } = null!;
    public BankAccount? BankAccount { get; set; }
}
