using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantIngredientStock : BaseEntity
{
    public int RestaurantIngredientId { get; set; }
    public decimal Quantity { get; set; }

    public RestaurantIngredient Ingredient { get; set; } = null!;
}
