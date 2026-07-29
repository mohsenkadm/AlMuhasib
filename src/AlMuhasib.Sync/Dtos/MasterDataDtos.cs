using AlMuhasib.Core.Enums;

namespace AlMuhasib.Sync.Dtos;

public sealed class CategorySyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
}

public sealed class ProductSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public Guid CategorySyncId { get; set; }
}

public sealed class PricingTypeSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductPriceSyncDto : SyncDtoBase
{
    public Guid ProductSyncId { get; set; }
    public Guid PricingTypeSyncId { get; set; }
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
}

public sealed class BusinessSettingsSyncDto : SyncDtoBase
{
    public bool ProductPricingEnabled { get; set; }
    public bool UpdateProductPriceOnPurchase { get; set; }
}

public sealed class WarehouseSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public sealed class CustomerSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? FileNumber { get; set; }
    public string? Notes { get; set; }
}

public sealed class SupplierSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public sealed class CashBoxSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public sealed class BankAccountSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
}

public sealed class InvestorSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ProfitPercentage { get; set; }
}

public sealed class ExpenseTypeSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
}

public sealed class PrintBrandingSettingsSyncDto : SyncDtoBase
{
    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhonePrimary { get; set; } = string.Empty;
    public string PhoneSecondary { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool ShowHeaderText { get; set; } = true;
    public bool ShowHeaderImage { get; set; }
    public byte[]? HeaderImageData { get; set; }
    public string? HeaderImageContentType { get; set; }
    public bool ShowFooterText { get; set; } = true;
    public string FooterText { get; set; } = string.Empty;
    public bool ShowFooterImage { get; set; }
    public byte[]? FooterImageData { get; set; }
    public string? FooterImageContentType { get; set; }
}

public sealed class WarehouseStockSyncDto : SyncDtoBase
{
    public Guid WarehouseSyncId { get; set; }
    public Guid ProductSyncId { get; set; }
    public decimal Quantity { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal MinQuantity { get; set; }
}
