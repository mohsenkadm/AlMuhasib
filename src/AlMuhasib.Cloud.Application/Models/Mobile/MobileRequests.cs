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
