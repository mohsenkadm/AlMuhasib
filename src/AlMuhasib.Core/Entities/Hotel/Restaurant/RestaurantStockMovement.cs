using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantStockMovement : BaseEntity
{
    public int RestaurantIngredientId { get; set; }
    public RestaurantStockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public int? RestaurantOrderId { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.Now;
    public string Notes { get; set; } = string.Empty;

    public RestaurantIngredient Ingredient { get; set; } = null!;
    public RestaurantOrder? Order { get; set; }
}
