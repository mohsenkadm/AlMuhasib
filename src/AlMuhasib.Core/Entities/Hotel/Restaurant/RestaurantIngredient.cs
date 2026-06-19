using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantIngredient : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "وحدة";
    public decimal MinQuantity { get; set; }
    public decimal AverageCost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public RestaurantIngredientStock? Stock { get; set; }
    public ICollection<RestaurantRecipeLine> RecipeLines { get; set; } = [];
    public ICollection<RestaurantStockMovement> StockMovements { get; set; } = [];
}
