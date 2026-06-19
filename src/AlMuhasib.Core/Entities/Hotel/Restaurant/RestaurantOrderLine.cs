using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantOrderLine : BaseEntity
{
    public int RestaurantOrderId { get; set; }
    public int RestaurantMenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal CogsAmount { get; set; }
    public string Notes { get; set; } = string.Empty;

    public RestaurantOrder Order { get; set; } = null!;
    public RestaurantMenuItem MenuItem { get; set; } = null!;
}
