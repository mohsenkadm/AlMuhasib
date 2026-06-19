using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed partial class SyncEngine
{
    private async Task<int> AcceptRestaurantPushAsync(int tenantId, SyncPushRequest request, SyncIdResolver resolver, SyncPushResponse response, CancellationToken ct)
    {
        var accepted = 0;

        foreach (var dto in request.Data.RestaurantIngredients)
            accepted += await UpsertRestaurantIngredientAsync(tenantId, dto, response, ct);
        await _db.SaveChangesAsync(ct);
        await FlushAndCacheAsync(_db.RestaurantIngredients, tenantId, request.Data.RestaurantIngredients.Select(i => i.SyncId), resolver, ct);

        foreach (var dto in request.Data.RestaurantIngredientStocks)
        {
            var ingId = await resolver.ResolveRestaurantIngredientAsync(dto.RestaurantIngredientSyncId, ct);
            if (ingId is null) { AddConflict(response, "RestaurantIngredientStock", dto.SyncId, "Ingredient not found"); continue; }
            accepted += await UpsertRestaurantIngredientStockAsync(tenantId, dto, ingId.Value, response, ct);
        }

        foreach (var dto in request.Data.RestaurantMenuCategories)
            accepted += await UpsertRestaurantMenuCategoryAsync(tenantId, dto, response, ct);
        await FlushAndCacheAsync(_db.RestaurantMenuCategories, tenantId, request.Data.RestaurantMenuCategories.Select(c => c.SyncId), resolver, ct);

        foreach (var dto in request.Data.RestaurantRecipes)
            accepted += await UpsertRestaurantRecipeAsync(tenantId, dto, response, ct);
        await FlushAndCacheAsync(_db.RestaurantRecipes, tenantId, request.Data.RestaurantRecipes.Select(r => r.SyncId), resolver, ct);

        foreach (var dto in request.Data.RestaurantMenuItems)
        {
            var catId = await resolver.ResolveRestaurantMenuCategoryAsync(dto.RestaurantMenuCategorySyncId, ct);
            if (catId is null) { AddConflict(response, "RestaurantMenuItem", dto.SyncId, "Category not found"); continue; }
            var recipeId = await resolver.ResolveRestaurantRecipeAsync(dto.RecipeSyncId, ct);
            accepted += await UpsertRestaurantMenuItemAsync(tenantId, dto, catId.Value, recipeId, response, ct);
        }
        await FlushAndCacheAsync(_db.RestaurantMenuItems, tenantId, request.Data.RestaurantMenuItems.Select(m => m.SyncId), resolver, ct);

        foreach (var dto in request.Data.RestaurantRecipeLines)
        {
            var recipeId = await resolver.ResolveRestaurantRecipeAsync(dto.RestaurantRecipeSyncId, ct);
            var ingId = await resolver.ResolveRestaurantIngredientAsync(dto.RestaurantIngredientSyncId, ct);
            if (recipeId is null || ingId is null) { AddConflict(response, "RestaurantRecipeLine", dto.SyncId, "Recipe or ingredient not found"); continue; }
            accepted += await UpsertRestaurantRecipeLineAsync(tenantId, dto, recipeId.Value, ingId.Value, response, ct);
        }

        foreach (var dto in request.Data.RestaurantTables)
            accepted += await UpsertRestaurantTableAsync(tenantId, dto, response, ct);
        await FlushAndCacheAsync(_db.RestaurantTables, tenantId, request.Data.RestaurantTables.Select(t => t.SyncId), resolver, ct);

        foreach (var dto in request.Data.RestaurantOrders)
        {
            var tableId = await resolver.ResolveRestaurantTableAsync(dto.RestaurantTableSyncId, ct);
            var reservationId = await resolver.ResolveHotelReservationAsync(dto.ReservationSyncId, ct);
            var roomId = await resolver.ResolveHotelRoomAsync(dto.RoomSyncId, ct);
            var guestId = await resolver.ResolveHotelGuestAsync(dto.GuestSyncId, ct);
            accepted += await UpsertRestaurantOrderAsync(tenantId, dto, tableId, reservationId, roomId, guestId, response, ct);
        }
        await FlushAndCacheAsync(_db.RestaurantOrders, tenantId, request.Data.RestaurantOrders.Select(o => o.SyncId), resolver, ct);

        foreach (var dto in request.Data.RestaurantOrderLines)
        {
            var orderId = await resolver.ResolveRestaurantOrderAsync(dto.RestaurantOrderSyncId, ct);
            var itemId = await resolver.ResolveRestaurantMenuItemAsync(dto.RestaurantMenuItemSyncId, ct);
            if (orderId is null || itemId is null) { AddConflict(response, "RestaurantOrderLine", dto.SyncId, "Order or item not found"); continue; }
            accepted += await UpsertRestaurantOrderLineAsync(tenantId, dto, orderId.Value, itemId.Value, response, ct);
        }

        foreach (var dto in request.Data.RestaurantOrderPayments)
        {
            var orderId = await resolver.ResolveRestaurantOrderAsync(dto.RestaurantOrderSyncId, ct);
            if (orderId is null) { AddConflict(response, "RestaurantOrderPayment", dto.SyncId, "Order not found"); continue; }
            var cashBoxId = await resolver.ResolveHotelCashBoxAsync(dto.HotelCashBoxSyncId, ct);
            accepted += await UpsertRestaurantOrderPaymentAsync(tenantId, dto, orderId.Value, cashBoxId, response, ct);
        }

        foreach (var dto in request.Data.RestaurantStockMovements)
        {
            var ingId = await resolver.ResolveRestaurantIngredientAsync(dto.RestaurantIngredientSyncId, ct);
            if (ingId is null) { AddConflict(response, "RestaurantStockMovement", dto.SyncId, "Ingredient not found"); continue; }
            var orderId = await resolver.ResolveRestaurantOrderAsync(dto.RestaurantOrderSyncId, ct);
            accepted += await UpsertRestaurantStockMovementAsync(tenantId, dto, ingId.Value, orderId, response, ct);
        }

        return accepted;
    }

    private async Task AppendRestaurantPullBundleAsync(int tenantId, SyncDataBundle bundle, DateTime since, CancellationToken ct)
    {
        bundle.RestaurantIngredients = await PullEntitiesAsync(_db.RestaurantIngredients, tenantId, since, MapRestaurantIngredient, ct);
        bundle.RestaurantMenuCategories = await PullEntitiesAsync(_db.RestaurantMenuCategories, tenantId, since, MapRestaurantMenuCategory, ct);
        bundle.RestaurantRecipes = await PullEntitiesAsync(_db.RestaurantRecipes, tenantId, since, MapRestaurantRecipe, ct);
        bundle.RestaurantTables = await PullEntitiesAsync(_db.RestaurantTables, tenantId, since, MapRestaurantTable, ct);

        var ingredientMap = await _db.RestaurantIngredients.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var categoryMap = await _db.RestaurantMenuCategories.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var recipeMap = await _db.RestaurantRecipes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var tableMap = await _db.RestaurantTables.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var menuItemMap = await _db.RestaurantMenuItems.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var orderMap = await _db.RestaurantOrders.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var roomMap = await _db.HotelRooms.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var guestMap = await _db.HotelGuests.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var reservationMap = await _db.HotelReservations.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var cashBoxMap = await _db.HotelCashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);

        var stocks = await _db.RestaurantIngredientStocks.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RestaurantIngredientStocks = stocks.Select(s => new RestaurantIngredientStockSyncDto
        {
            SyncId = s.SyncId, CreatedAt = s.CreatedAt, CreatedBy = s.CreatedBy, UpdatedAt = s.UpdatedAt, UpdatedBy = s.UpdatedBy,
            IsDeleted = s.IsDeleted, DeletedAt = s.DeletedAt, DeletedBy = s.DeletedBy, RowVersion = s.RowVersion,
            RestaurantIngredientSyncId = ingredientMap.GetValueOrDefault(s.RestaurantIngredientId), Quantity = s.Quantity
        }).ToList();

        var menuItems = await _db.RestaurantMenuItems.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RestaurantMenuItems = menuItems.Select(m => new RestaurantMenuItemSyncDto
        {
            SyncId = m.SyncId, CreatedAt = m.CreatedAt, CreatedBy = m.CreatedBy, UpdatedAt = m.UpdatedAt, UpdatedBy = m.UpdatedBy,
            IsDeleted = m.IsDeleted, DeletedAt = m.DeletedAt, DeletedBy = m.DeletedBy, RowVersion = m.RowVersion,
            RestaurantMenuCategorySyncId = categoryMap.GetValueOrDefault(m.RestaurantMenuCategoryId),
            Name = m.Name, Barcode = m.Barcode, SalePrice = m.SalePrice,
            RecipeSyncId = m.RecipeId.HasValue ? recipeMap.GetValueOrDefault(m.RecipeId.Value) : null,
            IsActive = m.IsActive, SortOrder = m.SortOrder, Notes = m.Notes
        }).ToList();

        var recipeLines = await _db.RestaurantRecipeLines.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RestaurantRecipeLines = recipeLines.Select(l => new RestaurantRecipeLineSyncDto
        {
            SyncId = l.SyncId, CreatedAt = l.CreatedAt, CreatedBy = l.CreatedBy, UpdatedAt = l.UpdatedAt, UpdatedBy = l.UpdatedBy,
            IsDeleted = l.IsDeleted, DeletedAt = l.DeletedAt, DeletedBy = l.DeletedBy, RowVersion = l.RowVersion,
            RestaurantRecipeSyncId = recipeMap.GetValueOrDefault(l.RestaurantRecipeId),
            RestaurantIngredientSyncId = ingredientMap.GetValueOrDefault(l.RestaurantIngredientId), Quantity = l.Quantity
        }).ToList();

        var orders = await _db.RestaurantOrders.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RestaurantOrders = orders.Select(o => new RestaurantOrderSyncDto
        {
            SyncId = o.SyncId, CreatedAt = o.CreatedAt, CreatedBy = o.CreatedBy, UpdatedAt = o.UpdatedAt, UpdatedBy = o.UpdatedBy,
            IsDeleted = o.IsDeleted, DeletedAt = o.DeletedAt, DeletedBy = o.DeletedBy, RowVersion = o.RowVersion,
            OrderNumber = o.OrderNumber, OrderType = o.OrderType, Status = o.Status, KitchenStatus = o.KitchenStatus,
            RestaurantTableSyncId = o.RestaurantTableId.HasValue ? tableMap.GetValueOrDefault(o.RestaurantTableId.Value) : null,
            ReservationSyncId = o.ReservationId.HasValue ? reservationMap.GetValueOrDefault(o.ReservationId.Value) : null,
            RoomSyncId = o.RoomId.HasValue ? roomMap.GetValueOrDefault(o.RoomId.Value) : null,
            GuestSyncId = o.GuestId.HasValue ? guestMap.GetValueOrDefault(o.GuestId.Value) : null,
            SubTotal = o.SubTotal, DiscountAmount = o.DiscountAmount, TotalAmount = o.TotalAmount,
            CogsAmount = o.CogsAmount, GrossProfit = o.GrossProfit, OrderDate = o.OrderDate, PaidAt = o.PaidAt, Notes = o.Notes
        }).ToList();

        var lines = await _db.RestaurantOrderLines.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RestaurantOrderLines = lines.Select(l => new RestaurantOrderLineSyncDto
        {
            SyncId = l.SyncId, CreatedAt = l.CreatedAt, CreatedBy = l.CreatedBy, UpdatedAt = l.UpdatedAt, UpdatedBy = l.UpdatedBy,
            IsDeleted = l.IsDeleted, DeletedAt = l.DeletedAt, DeletedBy = l.DeletedBy, RowVersion = l.RowVersion,
            RestaurantOrderSyncId = orderMap.GetValueOrDefault(l.RestaurantOrderId),
            RestaurantMenuItemSyncId = menuItemMap.GetValueOrDefault(l.RestaurantMenuItemId),
            ItemName = l.ItemName, Quantity = l.Quantity, UnitPrice = l.UnitPrice, DiscountAmount = l.DiscountAmount,
            LineTotal = l.LineTotal, CogsAmount = l.CogsAmount, Notes = l.Notes
        }).ToList();

        var payments = await _db.RestaurantOrderPayments.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RestaurantOrderPayments = payments.Select(p => new RestaurantOrderPaymentSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            RestaurantOrderSyncId = orderMap.GetValueOrDefault(p.RestaurantOrderId), Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            HotelCashBoxSyncId = p.HotelCashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(p.HotelCashBoxId.Value) : null,
            Notes = p.Notes
        }).ToList();

        var movements = await _db.RestaurantStockMovements.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.RestaurantStockMovements = movements.Select(m => new RestaurantStockMovementSyncDto
        {
            SyncId = m.SyncId, CreatedAt = m.CreatedAt, CreatedBy = m.CreatedBy, UpdatedAt = m.UpdatedAt, UpdatedBy = m.UpdatedBy,
            IsDeleted = m.IsDeleted, DeletedAt = m.DeletedAt, DeletedBy = m.DeletedBy, RowVersion = m.RowVersion,
            RestaurantIngredientSyncId = ingredientMap.GetValueOrDefault(m.RestaurantIngredientId),
            MovementType = m.MovementType, Quantity = m.Quantity, UnitCost = m.UnitCost,
            RestaurantOrderSyncId = m.RestaurantOrderId.HasValue ? orderMap.GetValueOrDefault(m.RestaurantOrderId.Value) : null,
            MovementDate = m.MovementDate, Notes = m.Notes
        }).ToList();
    }

    private static RestaurantIngredientSyncDto MapRestaurantIngredient(CloudRestaurantIngredient e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Unit = e.Unit, MinQuantity = e.MinQuantity, AverageCost = e.AverageCost, Notes = e.Notes, IsActive = e.IsActive
    };

    private static RestaurantMenuCategorySyncDto MapRestaurantMenuCategory(CloudRestaurantMenuCategory e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, SortOrder = e.SortOrder, ColorHex = e.ColorHex, IsActive = e.IsActive
    };

    private static RestaurantRecipeSyncDto MapRestaurantRecipe(CloudRestaurantRecipe e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Notes = e.Notes
    };

    private static RestaurantTableSyncDto MapRestaurantTable(CloudRestaurantTable e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        TableNumber = e.TableNumber, Capacity = e.Capacity, Status = e.Status, SortOrder = e.SortOrder, Notes = e.Notes, IsActive = e.IsActive
    };

    private async Task<int> UpsertRestaurantIngredientAsync(int tenantId, RestaurantIngredientSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantIngredients, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantIngredient", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantIngredient { TenantId = tenantId }; _db.RestaurantIngredients.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name; existing.Unit = dto.Unit; existing.MinQuantity = dto.MinQuantity;
        existing.AverageCost = dto.AverageCost; existing.Notes = dto.Notes; existing.IsActive = dto.IsActive;
        return 1;
    }

    private async Task<int> UpsertRestaurantIngredientStockAsync(int tenantId, RestaurantIngredientStockSyncDto dto, int ingId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantIngredientStocks, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantIngredientStock", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantIngredientStock { TenantId = tenantId }; _db.RestaurantIngredientStocks.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RestaurantIngredientId = ingId; existing.Quantity = dto.Quantity;
        return 1;
    }

    private async Task<int> UpsertRestaurantMenuCategoryAsync(int tenantId, RestaurantMenuCategorySyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantMenuCategories, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantMenuCategory", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantMenuCategory { TenantId = tenantId }; _db.RestaurantMenuCategories.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name; existing.SortOrder = dto.SortOrder; existing.ColorHex = dto.ColorHex; existing.IsActive = dto.IsActive;
        return 1;
    }

    private async Task<int> UpsertRestaurantRecipeAsync(int tenantId, RestaurantRecipeSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantRecipes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantRecipe", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantRecipe { TenantId = tenantId }; _db.RestaurantRecipes.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertRestaurantMenuItemAsync(int tenantId, RestaurantMenuItemSyncDto dto, int catId, int? recipeId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantMenuItems, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantMenuItem", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantMenuItem { TenantId = tenantId }; _db.RestaurantMenuItems.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RestaurantMenuCategoryId = catId; existing.Name = dto.Name; existing.Barcode = dto.Barcode;
        existing.SalePrice = dto.SalePrice; existing.RecipeId = recipeId; existing.IsActive = dto.IsActive;
        existing.SortOrder = dto.SortOrder; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertRestaurantRecipeLineAsync(int tenantId, RestaurantRecipeLineSyncDto dto, int recipeId, int ingId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantRecipeLines, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantRecipeLine", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantRecipeLine { TenantId = tenantId }; _db.RestaurantRecipeLines.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RestaurantRecipeId = recipeId; existing.RestaurantIngredientId = ingId; existing.Quantity = dto.Quantity;
        return 1;
    }

    private async Task<int> UpsertRestaurantTableAsync(int tenantId, RestaurantTableSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantTables, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantTable", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantTable { TenantId = tenantId }; _db.RestaurantTables.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.TableNumber = dto.TableNumber; existing.Capacity = dto.Capacity; existing.Status = dto.Status;
        existing.SortOrder = dto.SortOrder; existing.Notes = dto.Notes; existing.IsActive = dto.IsActive;
        return 1;
    }

    private async Task<int> UpsertRestaurantOrderAsync(int tenantId, RestaurantOrderSyncDto dto, int? tableId, int? reservationId, int? roomId, int? guestId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantOrders, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantOrder", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantOrder { TenantId = tenantId }; _db.RestaurantOrders.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.OrderNumber = dto.OrderNumber; existing.OrderType = dto.OrderType; existing.Status = dto.Status;
        existing.KitchenStatus = dto.KitchenStatus; existing.RestaurantTableId = tableId;
        existing.ReservationId = reservationId; existing.RoomId = roomId; existing.GuestId = guestId;
        existing.SubTotal = dto.SubTotal; existing.DiscountAmount = dto.DiscountAmount; existing.TotalAmount = dto.TotalAmount;
        existing.CogsAmount = dto.CogsAmount; existing.GrossProfit = dto.GrossProfit; existing.OrderDate = dto.OrderDate;
        existing.PaidAt = dto.PaidAt; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertRestaurantOrderLineAsync(int tenantId, RestaurantOrderLineSyncDto dto, int orderId, int itemId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantOrderLines, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantOrderLine", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantOrderLine { TenantId = tenantId }; _db.RestaurantOrderLines.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RestaurantOrderId = orderId; existing.RestaurantMenuItemId = itemId;
        existing.ItemName = dto.ItemName; existing.Quantity = dto.Quantity; existing.UnitPrice = dto.UnitPrice;
        existing.DiscountAmount = dto.DiscountAmount; existing.LineTotal = dto.LineTotal; existing.CogsAmount = dto.CogsAmount;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertRestaurantOrderPaymentAsync(int tenantId, RestaurantOrderPaymentSyncDto dto, int orderId, int? cashBoxId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantOrderPayments, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantOrderPayment", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantOrderPayment { TenantId = tenantId }; _db.RestaurantOrderPayments.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RestaurantOrderId = orderId; existing.Amount = dto.Amount; existing.PaymentMethod = dto.PaymentMethod;
        existing.HotelCashBoxId = cashBoxId; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertRestaurantStockMovementAsync(int tenantId, RestaurantStockMovementSyncDto dto, int ingId, int? orderId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.RestaurantStockMovements, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "RestaurantStockMovement", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudRestaurantStockMovement { TenantId = tenantId }; _db.RestaurantStockMovements.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RestaurantIngredientId = ingId; existing.MovementType = dto.MovementType;
        existing.Quantity = dto.Quantity; existing.UnitCost = dto.UnitCost; existing.RestaurantOrderId = orderId;
        existing.MovementDate = dto.MovementDate; existing.Notes = dto.Notes;
        return 1;
    }
}
