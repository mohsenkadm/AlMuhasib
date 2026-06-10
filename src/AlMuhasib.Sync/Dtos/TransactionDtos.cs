using AlMuhasib.Core.Enums;

namespace AlMuhasib.Sync.Dtos;

public sealed class InvoiceSyncDto : SyncDtoBase
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public Guid? CustomerSyncId { get; set; }
    public Guid? SupplierSyncId { get; set; }
    public Guid WarehouseSyncId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public RoundingType RoundingType { get; set; }
    public Guid? CashBoxSyncId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CreditDueDate { get; set; }
    public string? Notes { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsCreditPaid { get; set; }
}

public sealed class InvoiceItemSyncDto : SyncDtoBase
{
    public Guid InvoiceSyncId { get; set; }
    public Guid? ProductSyncId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public sealed class InstallmentPlanSyncDto : SyncDtoBase
{
    public Guid InvoiceSyncId { get; set; }
    public Guid CustomerSyncId { get; set; }
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public InstallmentType InstallmentType { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
}

public sealed class InstallmentSyncDto : SyncDtoBase
{
    public Guid InstallmentPlanSyncId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InstallmentStatus Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public Guid? CashBoxSyncId { get; set; }
}

public sealed class VoucherSyncDto : SyncDtoBase
{
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public decimal Amount { get; set; }
    public decimal BankFees { get; set; }
    public Guid? CustomerSyncId { get; set; }
    public Guid? InvestorSyncId { get; set; }
    public Guid CashBoxSyncId { get; set; }
    public Guid? BankAccountSyncId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

public sealed class ExpenseSyncDto : SyncDtoBase
{
    public Guid ExpenseTypeSyncId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Guid CashBoxSyncId { get; set; }
    public string? Notes { get; set; }
}

public sealed class TransferSyncDto : SyncDtoBase
{
    public TransferAccountType FromType { get; set; }
    public Guid FromSyncId { get; set; }
    public TransferAccountType ToType { get; set; }
    public Guid ToSyncId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

public sealed class InvestorTransactionSyncDto : SyncDtoBase
{
    public Guid InvestorSyncId { get; set; }
    public InvestorTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProfitDistributionSyncDto : SyncDtoBase
{
    public DateTime Date { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal DistributedAmount { get; set; }
}

public sealed class ProfitDistributionDetailSyncDto : SyncDtoBase
{
    public Guid ProfitDistributionSyncId { get; set; }
    public Guid InvestorSyncId { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal Amount { get; set; }
}

public sealed class CapitalEntrySyncDto : SyncDtoBase
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public CapitalEntryType Type { get; set; }
    public string? Notes { get; set; }
}

public sealed class CustomerAttachmentSyncDto : SyncDtoBase
{
    public Guid CustomerSyncId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[]? FileData { get; set; }
}
