using AlMuhasib.Core.Enums;

namespace AlMuhasib.Api.Models;

public sealed class VoucherListItem
{
    public Guid SyncId { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public decimal Amount { get; set; }
    public decimal BankFees { get; set; }
    public Guid? CustomerSyncId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? InvestorSyncId { get; set; }
    public string? InvestorName { get; set; }
    public Guid CashBoxSyncId { get; set; }
    public string CashBoxName { get; set; } = string.Empty;
    public Guid? BankAccountSyncId { get; set; }
    public string? BankAccountName { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

public sealed class ExpenseListItem
{
    public Guid SyncId { get; set; }
    public Guid ExpenseTypeSyncId { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Guid CashBoxSyncId { get; set; }
    public string CashBoxName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class TransferListItem
{
    public Guid SyncId { get; set; }
    public TransferAccountType FromType { get; set; }
    public Guid? FromSyncId { get; set; }
    public string FromName { get; set; } = string.Empty;
    public TransferAccountType ToType { get; set; }
    public Guid? ToSyncId { get; set; }
    public string ToName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

public sealed class WarehouseStockListItem
{
    public Guid SyncId { get; set; }
    public Guid WarehouseSyncId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid ProductSyncId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal UnitCost { get; set; }
}

public sealed class WarehouseTransferListItem
{
    public Guid SyncId { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public Guid FromWarehouseSyncId { get; set; }
    public string FromWarehouseName { get; set; } = string.Empty;
    public Guid ToWarehouseSyncId { get; set; }
    public string ToWarehouseName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public List<WarehouseTransferItemListItem> Items { get; set; } = [];
}

public sealed class WarehouseTransferItemListItem
{
    public Guid SyncId { get; set; }
    public Guid ProductSyncId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public sealed class InstallmentListItem
{
    public Guid SyncId { get; set; }
    public Guid PlanSyncId { get; set; }
    public Guid CustomerSyncId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? FileNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InstallmentStatus Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public Guid? CashBoxSyncId { get; set; }
    public string? CashBoxName { get; set; }
}

public sealed class InstallmentPlanDetailResponse
{
    public Guid SyncId { get; set; }
    public Guid InvoiceSyncId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerSyncId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public InstallmentType InstallmentType { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    public List<InstallmentListItem> Installments { get; set; } = [];
}
