using AlMuhasib.Core.Entities.Hotel.Restaurant;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IRestaurantMenuService
{
    Task EnsureSeedDataAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RestaurantMenuCategory>> GetCategoriesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<RestaurantMenuCategory> SaveCategoryAsync(RestaurantMenuCategory category, CancellationToken ct = default);
    Task DeleteCategoryAsync(int id, string deletedBy, CancellationToken ct = default);
    Task<IReadOnlyList<RestaurantMenuItem>> GetMenuItemsAsync(int? categoryId = null, bool activeOnly = true, CancellationToken ct = default);
    Task<RestaurantMenuItem?> GetMenuItemByIdAsync(int id, CancellationToken ct = default);
    Task<RestaurantMenuItem> SaveMenuItemAsync(RestaurantMenuItem item, RestaurantRecipe? recipe, IReadOnlyList<RestaurantRecipeLine>? lines, CancellationToken ct = default);
    Task DeleteMenuItemAsync(int id, string deletedBy, CancellationToken ct = default);
    Task<RestaurantRecipe?> GetRecipeForMenuItemAsync(int menuItemId, CancellationToken ct = default);
}
