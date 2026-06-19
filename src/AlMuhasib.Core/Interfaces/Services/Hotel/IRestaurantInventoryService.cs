using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IRestaurantInventoryService
{
    Task<IReadOnlyList<RestaurantIngredient>> GetIngredientsAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<RestaurantIngredient?> GetIngredientByIdAsync(int id, CancellationToken ct = default);
    Task<RestaurantIngredient> CreateIngredientAsync(RestaurantIngredient ingredient, decimal initialQuantity = 0, CancellationToken ct = default);
    Task<RestaurantIngredient> UpdateIngredientAsync(RestaurantIngredient ingredient, CancellationToken ct = default);
    Task DeleteIngredientAsync(int id, string deletedBy, CancellationToken ct = default);
    Task<RestaurantIngredientStock?> GetStockAsync(int ingredientId, CancellationToken ct = default);
    Task PurchaseStockAsync(int ingredientId, decimal quantity, decimal unitCost, int? cashBoxId, string notes, CancellationToken ct = default);
    Task AdjustStockAsync(int ingredientId, decimal newQuantity, string notes, CancellationToken ct = default);
    Task<IReadOnlyList<RestaurantIngredient>> GetLowStockAlertsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RestaurantStockMovement>> GetMovementsAsync(int? ingredientId, DateTime? from, DateTime? to, CancellationToken ct = default);
}
