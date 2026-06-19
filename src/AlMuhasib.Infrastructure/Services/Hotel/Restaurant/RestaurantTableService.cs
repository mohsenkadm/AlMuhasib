using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel.Restaurant;

public sealed class RestaurantTableService : IRestaurantTableService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public RestaurantTableService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<RestaurantTable>> GetTablesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var query = db.RestaurantTables.AsQueryable();
        if (activeOnly)
            query = query.Where(t => t.IsActive);
        return await query.OrderBy(t => t.SortOrder).ThenBy(t => t.TableNumber).ToListAsync(ct);
    }

    public async Task<RestaurantTable> SaveTableAsync(RestaurantTable table, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        if (table.Id == 0)
        {
            await db.RestaurantTables.AddAsync(table, ct);
        }
        else
        {
            var existing = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == table.Id, ct)
                ?? throw new InvalidOperationException("الطاولة غير موجودة");
            existing.TableNumber = table.TableNumber;
            existing.Capacity = table.Capacity;
            existing.SortOrder = table.SortOrder;
            existing.Notes = table.Notes;
            existing.IsActive = table.IsActive;
            table = existing;
        }

        await db.SaveChangesAsync(ct);
        return table;
    }

    public async Task DeleteTableAsync(int id, string deletedBy, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new InvalidOperationException("الطاولة غير موجودة");
        table.MarkSoftDeleted(deletedBy);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetTableStatusAsync(int tableId, RestaurantTableStatus status, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == tableId, ct)
            ?? throw new InvalidOperationException("الطاولة غير موجودة");
        table.Status = status;
        await db.SaveChangesAsync(ct);
    }
}
