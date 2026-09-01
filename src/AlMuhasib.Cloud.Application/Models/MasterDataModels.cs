namespace AlMuhasib.Cloud.Application.Models;

public class LookupItem
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Extra { get; set; }
    public string? FileNumber { get; set; }
    /// <summary>رصيد الزبون المستحق (للعملاء فقط).</summary>
    public decimal? Balance { get; set; }
}

public sealed class ProductLookupItem : LookupItem
{
    public string? Barcode { get; set; }
    public string? ScientificName { get; set; }
    public string? UsageInstructions { get; set; }
    public Guid CategorySyncId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<ProductPriceLookupItem> Prices { get; set; } = [];
}

public sealed class ProductPriceLookupItem
{
    public Guid SyncId { get; set; }
    public Guid ProductSyncId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid PricingTypeSyncId { get; set; }
    public string PricingTypeName { get; set; } = string.Empty;
    public bool IsDefaultPricingType { get; set; }
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
}

public sealed class PricingTypeLookupItem : LookupItem
{
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BusinessSettingsDto
{
    public Guid SyncId { get; set; }
    public bool ProductPricingEnabled { get; set; }
    public bool UpdateProductPriceOnPurchase { get; set; }
    public bool PeriodLockEnabled { get; set; }
    public DateTime? LockedThroughDate { get; set; }
}

public sealed class MasterDataBundle
{
    public List<LookupItem> Categories { get; set; } = [];
    public List<ProductLookupItem> Products { get; set; } = [];
    public List<PricingTypeLookupItem> PricingTypes { get; set; } = [];
    public List<LookupItem> Customers { get; set; } = [];
    public List<LookupItem> Suppliers { get; set; } = [];
    public List<LookupItem> Warehouses { get; set; } = [];
    public List<LookupItem> CashBoxes { get; set; } = [];
    public List<LookupItem> BankAccounts { get; set; } = [];
    public List<LookupItem> ExpenseTypes { get; set; } = [];
    public List<LookupItem> Investors { get; set; } = [];
    public BusinessSettingsDto? BusinessSettings { get; set; }
}

public sealed class ReportFilterRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? CustomerSyncId { get; set; }
    public Guid? SupplierSyncId { get; set; }
    public Guid? WarehouseSyncId { get; set; }
    public Guid? ProductSyncId { get; set; }
    public Guid? CashBoxSyncId { get; set; }
    public Guid? BankAccountSyncId { get; set; }
    public Guid? ExpenseTypeSyncId { get; set; }
    public Guid? InvestorSyncId { get; set; }
    public string? Status { get; set; }
    public int? TopCount { get; set; }
    public decimal? LowStockThreshold { get; set; }
    public int? DeadStockDays { get; set; }
    public string? StockHealthFilter { get; set; }
    public string? InventoryReplenishmentFilter { get; set; }
    public string? MinimumQuantityFilter { get; set; }
    public Guid? CategorySyncId { get; set; }
    public string? Search { get; set; }
    public DateTime? AsOfDate { get; set; }
    public int? MinDaysOverdue { get; set; }
}
