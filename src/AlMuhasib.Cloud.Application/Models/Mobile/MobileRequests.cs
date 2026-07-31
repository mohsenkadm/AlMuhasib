using AlMuhasib.Core.Enums;

namespace AlMuhasib.Cloud.Application.Models.Mobile;

public sealed class CreateCustomerRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public sealed class CreateSupplierRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public sealed class CreateProductRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? ScientificName { get; set; }
    public string? Description { get; set; }
    public Guid CategorySyncId { get; set; }
}

public sealed class CreateInvestorRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal OpeningBalance { get; set; }
}

public sealed class CreateInvoiceItemRequest
{
    public Guid? ProductSyncId { get; set; }
    public Guid? PricingTypeSyncId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}

public sealed class CreateInstallmentPlanRequest
{
    public int NumberOfInstallments { get; set; }
    public DateTime StartDate { get; set; }
    public InstallmentType InstallmentType { get; set; } = InstallmentType.Manual;
    public string? FileNumber { get; set; }
}

public sealed class CreateInvoiceRequest
{
    public Guid? SyncId { get; set; }
    public InvoiceType InvoiceType { get; set; }
    public Guid? CustomerSyncId { get; set; }
    public Guid? SupplierSyncId { get; set; }
    public Guid WarehouseSyncId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? CashBoxSyncId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CreditDueDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItemRequest> Items { get; set; } = [];
    public CreateInstallmentPlanRequest? InstallmentPlan { get; set; }
}

public sealed class UpsertPricingTypeRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpsertProductPriceRequest
{
    public Guid? SyncId { get; set; }
    public Guid ProductSyncId { get; set; }
    public Guid PricingTypeSyncId { get; set; }
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
}

public sealed class UpdateBusinessSettingsRequest
{
    public bool ProductPricingEnabled { get; set; }
    public bool UpdateProductPriceOnPurchase { get; set; }
}

public sealed class UpsertCashBoxRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
}

public sealed class UpsertBankAccountRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal OpeningBalance { get; set; }
}

public sealed class UpsertExpenseTypeRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateVoucherRequest
{
    public Guid? SyncId { get; set; }
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

public sealed class CreateExpenseRequest
{
    public Guid? SyncId { get; set; }
    public Guid ExpenseTypeSyncId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Guid CashBoxSyncId { get; set; }
    public string? Notes { get; set; }
}

public sealed class CreateTransferRequest
{
    public Guid? SyncId { get; set; }
    public TransferAccountType FromType { get; set; }
    public Guid FromSyncId { get; set; }
    public TransferAccountType ToType { get; set; }
    public Guid ToSyncId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpsertWarehouseRequest
{
    public Guid? SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public sealed class CreateWarehouseTransferItemRequest
{
    public Guid ProductSyncId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class CreateWarehouseTransferRequest
{
    public Guid? SyncId { get; set; }
    public Guid FromWarehouseSyncId { get; set; }
    public Guid ToWarehouseSyncId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public List<CreateWarehouseTransferItemRequest> Items { get; set; } = [];
}

public sealed class StockAdjustmentItemRequest
{
    public Guid ProductSyncId { get; set; }
    public decimal NewQuantity { get; set; }
}

public sealed class CreateStockAdjustmentRequest
{
    public Guid WarehouseSyncId { get; set; }
    public List<StockAdjustmentItemRequest> Items { get; set; } = [];
    public string? Notes { get; set; }
}

public sealed class PayInstallmentRequest
{
    public decimal Amount { get; set; }
    public Guid CashBoxSyncId { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }
}
