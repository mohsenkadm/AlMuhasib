using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Infrastructure.Data.Hotel;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

internal static partial class HotelSyncMapper
{
    private static async Task AppendRestaurantToBundleAsync(
        SyncDataBundle bundle,
        HotelDbContext db,
        Func<BaseEntity, bool> shouldSync,
        Dictionary<int, Guid> roomMap,
        Dictionary<int, Guid> guestMap,
        Dictionary<int, Guid> reservationMap,
        Dictionary<int, Guid> cashBoxMap,
        CancellationToken ct)
    {
        var ingredients = await db.RestaurantIngredients.IgnoreQueryFilters().ToListAsync(ct);
        var stocks = await db.RestaurantIngredientStocks.IgnoreQueryFilters().ToListAsync(ct);
        var categories = await db.RestaurantMenuCategories.IgnoreQueryFilters().ToListAsync(ct);
        var recipes = await db.RestaurantRecipes.IgnoreQueryFilters().ToListAsync(ct);
        var menuItems = await db.RestaurantMenuItems.IgnoreQueryFilters().ToListAsync(ct);
        var recipeLines = await db.RestaurantRecipeLines.IgnoreQueryFilters().ToListAsync(ct);
        var tables = await db.RestaurantTables.IgnoreQueryFilters().ToListAsync(ct);
        var orders = await db.RestaurantOrders.IgnoreQueryFilters().ToListAsync(ct);
        var orderLines = await db.RestaurantOrderLines.IgnoreQueryFilters().ToListAsync(ct);
        var orderPayments = await db.RestaurantOrderPayments.IgnoreQueryFilters().ToListAsync(ct);
        var movements = await db.RestaurantStockMovements.IgnoreQueryFilters().ToListAsync(ct);

        var ingredientMap = ingredients.ToDictionary(i => i.Id, i => i.SyncId);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.SyncId);
        var recipeMap = recipes.ToDictionary(r => r.Id, r => r.SyncId);
        var menuItemMap = menuItems.ToDictionary(m => m.Id, m => m.SyncId);
        var tableMap = tables.ToDictionary(t => t.Id, t => t.SyncId);
        var orderMap = orders.ToDictionary(o => o.Id, o => o.SyncId);

        bundle.RestaurantIngredients = ingredients.Where(i => shouldSync(i)).Select(i =>
        {
            var d = new RestaurantIngredientSyncDto(); CopyBase(i, d);
            d.Name = i.Name; d.Unit = i.Unit; d.MinQuantity = i.MinQuantity;
            d.AverageCost = i.AverageCost; d.Notes = i.Notes; d.IsActive = i.IsActive;
            return d;
        }).ToList();

        bundle.RestaurantIngredientStocks = stocks.Where(x => shouldSync(x)).Where(s => ingredientMap.ContainsKey(s.RestaurantIngredientId)).Select(s =>
        {
            var d = new RestaurantIngredientStockSyncDto(); CopyBase(s, d);
            d.RestaurantIngredientSyncId = ingredientMap[s.RestaurantIngredientId]; d.Quantity = s.Quantity;
            return d;
        }).ToList();

        bundle.RestaurantMenuCategories = categories.Where(x => shouldSync(x)).Select(c =>
        {
            var d = new RestaurantMenuCategorySyncDto(); CopyBase(c, d);
            d.Name = c.Name; d.SortOrder = c.SortOrder; d.ColorHex = c.ColorHex; d.IsActive = c.IsActive;
            return d;
        }).ToList();

        bundle.RestaurantRecipes = recipes.Where(x => shouldSync(x)).Select(r =>
        {
            var d = new RestaurantRecipeSyncDto(); CopyBase(r, d);
            d.Name = r.Name; d.Notes = r.Notes;
            return d;
        }).ToList();

        bundle.RestaurantMenuItems = menuItems.Where(x => shouldSync(x)).Where(m => categoryMap.ContainsKey(m.RestaurantMenuCategoryId)).Select(m =>
        {
            var d = new RestaurantMenuItemSyncDto(); CopyBase(m, d);
            d.RestaurantMenuCategorySyncId = categoryMap[m.RestaurantMenuCategoryId];
            d.Name = m.Name; d.Barcode = m.Barcode; d.SalePrice = m.SalePrice;
            d.RecipeSyncId = m.RecipeId.HasValue ? recipeMap.GetValueOrDefault(m.RecipeId.Value) : null;
            d.IsActive = m.IsActive; d.SortOrder = m.SortOrder; d.Notes = m.Notes;
            return d;
        }).ToList();

        bundle.RestaurantRecipeLines = recipeLines.Where(x => shouldSync(x))
            .Where(l => recipeMap.ContainsKey(l.RestaurantRecipeId) && ingredientMap.ContainsKey(l.RestaurantIngredientId))
            .Select(l =>
            {
                var d = new RestaurantRecipeLineSyncDto(); CopyBase(l, d);
                d.RestaurantRecipeSyncId = recipeMap[l.RestaurantRecipeId];
                d.RestaurantIngredientSyncId = ingredientMap[l.RestaurantIngredientId];
                d.Quantity = l.Quantity;
                return d;
            }).ToList();

        bundle.RestaurantTables = tables.Where(x => shouldSync(x)).Select(t =>
        {
            var d = new RestaurantTableSyncDto(); CopyBase(t, d);
            d.TableNumber = t.TableNumber; d.Capacity = t.Capacity; d.Status = t.Status;
            d.SortOrder = t.SortOrder; d.Notes = t.Notes; d.IsActive = t.IsActive;
            return d;
        }).ToList();

        bundle.RestaurantOrders = orders.Where(x => shouldSync(x)).Select(o =>
        {
            var d = new RestaurantOrderSyncDto(); CopyBase(o, d);
            d.OrderNumber = o.OrderNumber; d.OrderType = o.OrderType; d.Status = o.Status;
            d.KitchenStatus = o.KitchenStatus;
            d.RestaurantTableSyncId = o.RestaurantTableId.HasValue ? tableMap.GetValueOrDefault(o.RestaurantTableId.Value) : null;
            d.ReservationSyncId = o.ReservationId.HasValue ? reservationMap.GetValueOrDefault(o.ReservationId.Value) : null;
            d.RoomSyncId = o.RoomId.HasValue ? roomMap.GetValueOrDefault(o.RoomId.Value) : null;
            d.GuestSyncId = o.GuestId.HasValue ? guestMap.GetValueOrDefault(o.GuestId.Value) : null;
            d.SubTotal = o.SubTotal; d.DiscountAmount = o.DiscountAmount; d.TotalAmount = o.TotalAmount;
            d.CogsAmount = o.CogsAmount; d.GrossProfit = o.GrossProfit; d.OrderDate = o.OrderDate;
            d.PaidAt = o.PaidAt; d.Notes = o.Notes;
            return d;
        }).ToList();

        bundle.RestaurantOrderLines = orderLines.Where(x => shouldSync(x))
            .Where(l => orderMap.ContainsKey(l.RestaurantOrderId) && menuItemMap.ContainsKey(l.RestaurantMenuItemId))
            .Select(l =>
            {
                var d = new RestaurantOrderLineSyncDto(); CopyBase(l, d);
                d.RestaurantOrderSyncId = orderMap[l.RestaurantOrderId];
                d.RestaurantMenuItemSyncId = menuItemMap[l.RestaurantMenuItemId];
                d.ItemName = l.ItemName; d.Quantity = l.Quantity; d.UnitPrice = l.UnitPrice;
                d.DiscountAmount = l.DiscountAmount; d.LineTotal = l.LineTotal; d.CogsAmount = l.CogsAmount;
                d.Notes = l.Notes;
                return d;
            }).ToList();

        bundle.RestaurantOrderPayments = orderPayments.Where(x => shouldSync(x)).Where(p => orderMap.ContainsKey(p.RestaurantOrderId)).Select(p =>
        {
            var d = new RestaurantOrderPaymentSyncDto(); CopyBase(p, d);
            d.RestaurantOrderSyncId = orderMap[p.RestaurantOrderId];
            d.Amount = p.Amount; d.PaymentMethod = p.PaymentMethod;
            d.HotelCashBoxSyncId = p.HotelCashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(p.HotelCashBoxId.Value) : null;
            d.Notes = p.Notes;
            return d;
        }).ToList();

        bundle.RestaurantStockMovements = movements.Where(x => shouldSync(x)).Where(m => ingredientMap.ContainsKey(m.RestaurantIngredientId)).Select(m =>
        {
            var d = new RestaurantStockMovementSyncDto(); CopyBase(m, d);
            d.RestaurantIngredientSyncId = ingredientMap[m.RestaurantIngredientId];
            d.MovementType = m.MovementType; d.Quantity = m.Quantity; d.UnitCost = m.UnitCost;
            d.RestaurantOrderSyncId = m.RestaurantOrderId.HasValue ? orderMap.GetValueOrDefault(m.RestaurantOrderId.Value) : null;
            d.MovementDate = m.MovementDate; d.Notes = m.Notes;
            return d;
        }).ToList();
    }

    private static async Task ApplyRestaurantPullAsync(
        HotelDbContext db,
        SyncDataBundle data,
        Dictionary<Guid, int> roomMap,
        Dictionary<Guid, int> guestMap,
        Dictionary<Guid, int> reservationMap,
        Dictionary<Guid, int> cashBoxMap,
        CancellationToken ct)
    {
        var ingredientMap = new Dictionary<Guid, int>();
        foreach (var dto in data.RestaurantIngredients)
        {
            var entity = await db.RestaurantIngredients.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.SyncId == dto.SyncId, ct)
                ?? new RestaurantIngredient();
            if (entity.Id == 0) db.RestaurantIngredients.Add(entity);
            ApplyBase(entity, dto);
            entity.Name = dto.Name; entity.Unit = dto.Unit; entity.MinQuantity = dto.MinQuantity;
            entity.AverageCost = dto.AverageCost; entity.Notes = dto.Notes; entity.IsActive = dto.IsActive;
            await db.SaveChangesAsync(ct);
            ingredientMap[dto.SyncId] = entity.Id;
        }

        foreach (var dto in data.RestaurantIngredientStocks)
        {
            if (!ingredientMap.TryGetValue(dto.RestaurantIngredientSyncId, out var ingId)) continue;
            var entity = await db.RestaurantIngredientStocks.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.SyncId == dto.SyncId, ct)
                ?? new RestaurantIngredientStock();
            if (entity.Id == 0) db.RestaurantIngredientStocks.Add(entity);
            ApplyBase(entity, dto);
            entity.RestaurantIngredientId = ingId;
            entity.Quantity = dto.Quantity;
        }

        var categoryMap = new Dictionary<Guid, int>();
        foreach (var dto in data.RestaurantMenuCategories)
        {
            var entity = await db.RestaurantMenuCategories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.SyncId == dto.SyncId, ct)
                ?? new RestaurantMenuCategory();
            if (entity.Id == 0) db.RestaurantMenuCategories.Add(entity);
            ApplyBase(entity, dto);
            entity.Name = dto.Name; entity.SortOrder = dto.SortOrder; entity.ColorHex = dto.ColorHex; entity.IsActive = dto.IsActive;
            await db.SaveChangesAsync(ct);
            categoryMap[dto.SyncId] = entity.Id;
        }

        var recipeMap = new Dictionary<Guid, int>();
        foreach (var dto in data.RestaurantRecipes)
        {
            var entity = await db.RestaurantRecipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.SyncId == dto.SyncId, ct)
                ?? new RestaurantRecipe();
            if (entity.Id == 0) db.RestaurantRecipes.Add(entity);
            ApplyBase(entity, dto);
            entity.Name = dto.Name; entity.Notes = dto.Notes;
            await db.SaveChangesAsync(ct);
            recipeMap[dto.SyncId] = entity.Id;
        }

        var menuItemMap = new Dictionary<Guid, int>();
        foreach (var dto in data.RestaurantMenuItems)
        {
            if (!categoryMap.TryGetValue(dto.RestaurantMenuCategorySyncId, out var catId)) continue;
            var entity = await db.RestaurantMenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.SyncId == dto.SyncId, ct)
                ?? new RestaurantMenuItem();
            if (entity.Id == 0) db.RestaurantMenuItems.Add(entity);
            ApplyBase(entity, dto);
            entity.RestaurantMenuCategoryId = catId;
            entity.Name = dto.Name; entity.Barcode = dto.Barcode; entity.SalePrice = dto.SalePrice;
            entity.RecipeId = dto.RecipeSyncId.HasValue ? recipeMap.GetValueOrDefault(dto.RecipeSyncId.Value) : null;
            entity.IsActive = dto.IsActive; entity.SortOrder = dto.SortOrder; entity.Notes = dto.Notes;
            await db.SaveChangesAsync(ct);
            menuItemMap[dto.SyncId] = entity.Id;
        }

        foreach (var dto in data.RestaurantRecipeLines)
        {
            if (!recipeMap.TryGetValue(dto.RestaurantRecipeSyncId, out var recipeId)) continue;
            if (!ingredientMap.TryGetValue(dto.RestaurantIngredientSyncId, out var ingId)) continue;
            var entity = await db.RestaurantRecipeLines.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.SyncId == dto.SyncId, ct)
                ?? new RestaurantRecipeLine();
            if (entity.Id == 0) db.RestaurantRecipeLines.Add(entity);
            ApplyBase(entity, dto);
            entity.RestaurantRecipeId = recipeId;
            entity.RestaurantIngredientId = ingId;
            entity.Quantity = dto.Quantity;
        }

        var tableMap = new Dictionary<Guid, int>();
        foreach (var dto in data.RestaurantTables)
        {
            var entity = await db.RestaurantTables.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.SyncId == dto.SyncId, ct)
                ?? new RestaurantTable();
            if (entity.Id == 0) db.RestaurantTables.Add(entity);
            ApplyBase(entity, dto);
            entity.TableNumber = dto.TableNumber; entity.Capacity = dto.Capacity; entity.Status = dto.Status;
            entity.SortOrder = dto.SortOrder; entity.Notes = dto.Notes; entity.IsActive = dto.IsActive;
            await db.SaveChangesAsync(ct);
            tableMap[dto.SyncId] = entity.Id;
        }

        var orderMap = new Dictionary<Guid, int>();
        foreach (var dto in data.RestaurantOrders)
        {
            var entity = await db.RestaurantOrders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.SyncId == dto.SyncId, ct)
                ?? new RestaurantOrder();
            if (entity.Id == 0) db.RestaurantOrders.Add(entity);
            ApplyBase(entity, dto);
            entity.OrderNumber = dto.OrderNumber; entity.OrderType = dto.OrderType; entity.Status = dto.Status;
            entity.KitchenStatus = dto.KitchenStatus;
            entity.RestaurantTableId = dto.RestaurantTableSyncId.HasValue ? tableMap.GetValueOrDefault(dto.RestaurantTableSyncId.Value) : null;
            entity.ReservationId = dto.ReservationSyncId.HasValue ? reservationMap.GetValueOrDefault(dto.ReservationSyncId.Value) : null;
            entity.RoomId = dto.RoomSyncId.HasValue ? roomMap.GetValueOrDefault(dto.RoomSyncId.Value) : null;
            entity.GuestId = dto.GuestSyncId.HasValue ? guestMap.GetValueOrDefault(dto.GuestSyncId.Value) : null;
            entity.SubTotal = dto.SubTotal; entity.DiscountAmount = dto.DiscountAmount; entity.TotalAmount = dto.TotalAmount;
            entity.CogsAmount = dto.CogsAmount; entity.GrossProfit = dto.GrossProfit; entity.OrderDate = dto.OrderDate;
            entity.PaidAt = dto.PaidAt; entity.Notes = dto.Notes;
            await db.SaveChangesAsync(ct);
            orderMap[dto.SyncId] = entity.Id;
        }

        foreach (var dto in data.RestaurantOrderLines)
        {
            if (!orderMap.TryGetValue(dto.RestaurantOrderSyncId, out var orderId)) continue;
            if (!menuItemMap.TryGetValue(dto.RestaurantMenuItemSyncId, out var itemId)) continue;
            var entity = await db.RestaurantOrderLines.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.SyncId == dto.SyncId, ct)
                ?? new RestaurantOrderLine();
            if (entity.Id == 0) db.RestaurantOrderLines.Add(entity);
            ApplyBase(entity, dto);
            entity.RestaurantOrderId = orderId;
            entity.RestaurantMenuItemId = itemId;
            entity.ItemName = dto.ItemName; entity.Quantity = dto.Quantity; entity.UnitPrice = dto.UnitPrice;
            entity.DiscountAmount = dto.DiscountAmount; entity.LineTotal = dto.LineTotal; entity.CogsAmount = dto.CogsAmount;
            entity.Notes = dto.Notes;
        }

        foreach (var dto in data.RestaurantOrderPayments)
        {
            if (!orderMap.TryGetValue(dto.RestaurantOrderSyncId, out var orderId)) continue;
            var entity = await db.RestaurantOrderPayments.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct)
                ?? new RestaurantOrderPayment();
            if (entity.Id == 0) db.RestaurantOrderPayments.Add(entity);
            ApplyBase(entity, dto);
            entity.RestaurantOrderId = orderId;
            entity.Amount = dto.Amount; entity.PaymentMethod = dto.PaymentMethod;
            entity.HotelCashBoxId = dto.HotelCashBoxSyncId.HasValue ? cashBoxMap.GetValueOrDefault(dto.HotelCashBoxSyncId.Value) : null;
            entity.Notes = dto.Notes;
        }

        foreach (var dto in data.RestaurantStockMovements)
        {
            if (!ingredientMap.TryGetValue(dto.RestaurantIngredientSyncId, out var ingId)) continue;
            var entity = await db.RestaurantStockMovements.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.SyncId == dto.SyncId, ct)
                ?? new RestaurantStockMovement();
            if (entity.Id == 0) db.RestaurantStockMovements.Add(entity);
            ApplyBase(entity, dto);
            entity.RestaurantIngredientId = ingId;
            entity.MovementType = dto.MovementType; entity.Quantity = dto.Quantity; entity.UnitCost = dto.UnitCost;
            entity.RestaurantOrderId = dto.RestaurantOrderSyncId.HasValue ? orderMap.GetValueOrDefault(dto.RestaurantOrderSyncId.Value) : null;
            entity.MovementDate = dto.MovementDate; entity.Notes = dto.Notes;
        }
    }
}
