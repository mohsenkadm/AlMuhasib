using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantMenuItem : BaseEntity
{
    public int RestaurantMenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SalePrice { get; set; }
    public string? ImagePath { get; set; }
    public int? RecipeId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;

    public RestaurantMenuCategory Category { get; set; } = null!;
    public RestaurantRecipe? Recipe { get; set; }
}
