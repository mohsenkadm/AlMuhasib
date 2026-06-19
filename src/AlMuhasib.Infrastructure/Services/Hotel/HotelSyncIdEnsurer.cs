using AlMuhasib.Core.Entities;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

internal static class HotelSyncIdEnsurer
{
    public static async Task EnsureAllAsync(HotelDbContext db, CancellationToken ct = default)
    {
        var changed = false;
        changed |= await EnsureAsync(db.HotelSettings, ct);
        changed |= await EnsureAsync(db.Floors, ct);
        changed |= await EnsureAsync(db.RoomTypes, ct);
        changed |= await EnsureAsync(db.Rooms, ct);
        changed |= await EnsureAsync(db.Guests, ct);
        changed |= await EnsureAsync(db.Reservations, ct);
        changed |= await EnsureAsync(db.ReservationCharges, ct);
        changed |= await EnsureAsync(db.ReservationPayments, ct);
        changed |= await EnsureAsync(db.HotelCashBoxes, ct);
        changed |= await EnsureAsync(db.HotelVouchers, ct);
        changed |= await EnsureAsync(db.HotelExpenseTypes, ct);
        changed |= await EnsureAsync(db.HotelExpenses, ct);
        changed |= await EnsureAsync(db.RatePlans, ct);
        changed |= await EnsureAsync(db.RatePlanSeasons, ct);
        changed |= await EnsureAsync(db.HousekeepingTasks, ct);
        changed |= await EnsureAsync(db.RestaurantIngredients, ct);
        changed |= await EnsureAsync(db.RestaurantIngredientStocks, ct);
        changed |= await EnsureAsync(db.RestaurantMenuCategories, ct);
        changed |= await EnsureAsync(db.RestaurantRecipes, ct);
        changed |= await EnsureAsync(db.RestaurantMenuItems, ct);
        changed |= await EnsureAsync(db.RestaurantRecipeLines, ct);
        changed |= await EnsureAsync(db.RestaurantTables, ct);
        changed |= await EnsureAsync(db.RestaurantOrders, ct);
        changed |= await EnsureAsync(db.RestaurantOrderLines, ct);
        changed |= await EnsureAsync(db.RestaurantOrderPayments, ct);
        changed |= await EnsureAsync(db.RestaurantStockMovements, ct);

        if (changed)
            await db.SaveChangesAsync(ct);
    }

    private static async Task<bool> EnsureAsync<T>(DbSet<T> set, CancellationToken ct) where T : BaseEntity
    {
        var missing = await set.IgnoreQueryFilters()
            .Where(e => e.SyncId == Guid.Empty)
            .ToListAsync(ct);

        if (missing.Count == 0)
            return false;

        var now = DateTime.UtcNow;
        foreach (var entity in missing)
        {
            entity.SyncId = Guid.NewGuid();
            entity.UpdatedAt = now;
            entity.UpdatedBy ??= "Sync";
        }

        return true;
    }
}
