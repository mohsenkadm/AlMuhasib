using AlMuhasib.Core.Enums;

namespace AlMuhasib.Sync.Dtos;

public sealed class RestaurantIngredientSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal MinQuantity { get; set; }
    public decimal AverageCost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class RestaurantIngredientStockSyncDto : SyncDtoBase
{
    public Guid RestaurantIngredientSyncId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class RestaurantMenuCategorySyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ColorHex { get; set; } = "#00897B";
    public bool IsActive { get; set; } = true;
}

public sealed class RestaurantRecipeSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class RestaurantMenuItemSyncDto : SyncDtoBase
{
    public Guid RestaurantMenuCategorySyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SalePrice { get; set; }
    public Guid? RecipeSyncId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class RestaurantRecipeLineSyncDto : SyncDtoBase
{
    public Guid RestaurantRecipeSyncId { get; set; }
    public Guid RestaurantIngredientSyncId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class RestaurantTableSyncDto : SyncDtoBase
{
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; } = 4;
    public RestaurantTableStatus Status { get; set; }
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class RestaurantOrderSyncDto : SyncDtoBase
{
    public string OrderNumber { get; set; } = string.Empty;
    public RestaurantOrderType OrderType { get; set; }
    public RestaurantOrderStatus Status { get; set; }
    public RestaurantKitchenStatus KitchenStatus { get; set; }
    public Guid? RestaurantTableSyncId { get; set; }
    public Guid? ReservationSyncId { get; set; }
    public Guid? RoomSyncId { get; set; }
    public Guid? GuestSyncId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CogsAmount { get; set; }
    public decimal GrossProfit { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class RestaurantOrderLineSyncDto : SyncDtoBase
{
    public Guid RestaurantOrderSyncId { get; set; }
    public Guid RestaurantMenuItemSyncId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal CogsAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class RestaurantOrderPaymentSyncDto : SyncDtoBase
{
    public Guid RestaurantOrderSyncId { get; set; }
    public decimal Amount { get; set; }
    public RestaurantPaymentMethod PaymentMethod { get; set; }
    public Guid? HotelCashBoxSyncId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class RestaurantStockMovementSyncDto : SyncDtoBase
{
    public Guid RestaurantIngredientSyncId { get; set; }
    public RestaurantStockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public Guid? RestaurantOrderSyncId { get; set; }
    public DateTime MovementDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
