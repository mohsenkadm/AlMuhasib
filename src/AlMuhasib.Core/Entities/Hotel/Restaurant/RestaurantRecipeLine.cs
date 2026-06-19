using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantRecipeLine : BaseEntity
{
    public int RestaurantRecipeId { get; set; }
    public int RestaurantIngredientId { get; set; }
    public decimal Quantity { get; set; }

    public RestaurantRecipe Recipe { get; set; } = null!;
    public RestaurantIngredient Ingredient { get; set; } = null!;
}
