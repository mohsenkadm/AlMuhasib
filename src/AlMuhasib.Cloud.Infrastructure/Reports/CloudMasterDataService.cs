using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public sealed class CloudMasterDataService : ICloudMasterDataService
{
    private readonly CloudDbContext _db;

    public CloudMasterDataService(CloudDbContext db) => _db = db;

    public async Task<MasterDataBundle> GetAllAsync(CancellationToken ct = default) => new()
    {
        Categories = await GetCategoriesAsync(ct),
        Products = await GetProductsAsync(ct),
        Customers = await GetCustomersAsync(ct),
        Suppliers = await GetSuppliersAsync(ct),
        Warehouses = await GetWarehousesAsync(ct),
        CashBoxes = await GetCashBoxesAsync(ct),
        BankAccounts = await GetBankAccountsAsync(ct),
        ExpenseTypes = await GetExpenseTypesAsync(ct),
        Investors = await GetInvestorsAsync(ct)
    };

    public Task<List<LookupItem>> GetCategoriesAsync(CancellationToken ct = default) =>
        _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem { Id = c.Id, SyncId = c.SyncId, Name = c.Name })
            .ToListAsync(ct);

    public Task<List<ProductLookupItem>> GetProductsAsync(CancellationToken ct = default) =>
        _db.Products.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductLookupItem
            {
                Id = p.Id,
                SyncId = p.SyncId,
                Name = p.Name,
                Barcode = p.Barcode,
                CategorySyncId = p.Category.SyncId,
                CategoryName = p.Category.Name
            })
            .ToListAsync(ct);

    public Task<List<LookupItem>> GetCustomersAsync(CancellationToken ct = default) =>
        _db.Customers.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem { Id = c.Id, SyncId = c.SyncId, Name = c.Name, Extra = c.Phone })
            .ToListAsync(ct);

    public Task<List<LookupItem>> GetSuppliersAsync(CancellationToken ct = default) =>
        _db.Suppliers.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new LookupItem { Id = s.Id, SyncId = s.SyncId, Name = s.Name, Extra = s.Phone })
            .ToListAsync(ct);

    public Task<List<LookupItem>> GetWarehousesAsync(CancellationToken ct = default) =>
        _db.Warehouses.AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new LookupItem { Id = w.Id, SyncId = w.SyncId, Name = w.Name })
            .ToListAsync(ct);

    public Task<List<LookupItem>> GetCashBoxesAsync(CancellationToken ct = default) =>
        _db.CashBoxes.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem { Id = c.Id, SyncId = c.SyncId, Name = c.Name })
            .ToListAsync(ct);

    public Task<List<LookupItem>> GetBankAccountsAsync(CancellationToken ct = default) =>
        _db.BankAccounts.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new LookupItem { Id = b.Id, SyncId = b.SyncId, Name = b.Name })
            .ToListAsync(ct);

    public Task<List<LookupItem>> GetExpenseTypesAsync(CancellationToken ct = default) =>
        _db.ExpenseTypes.AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new LookupItem { Id = e.Id, SyncId = e.SyncId, Name = e.Name })
            .ToListAsync(ct);

    public Task<List<LookupItem>> GetInvestorsAsync(CancellationToken ct = default) =>
        _db.Investors.AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new LookupItem { Id = i.Id, SyncId = i.SyncId, Name = i.Name })
            .ToListAsync(ct);

    public Task<int?> ResolveIdBySyncIdAsync(string entityType, Guid syncId, CancellationToken ct = default) =>
        entityType.ToLowerInvariant() switch
        {
            "category" => ResolveAsync(_db.Categories, syncId, ct),
            "product" => ResolveAsync(_db.Products, syncId, ct),
            "customer" => ResolveAsync(_db.Customers, syncId, ct),
            "supplier" => ResolveAsync(_db.Suppliers, syncId, ct),
            "warehouse" => ResolveAsync(_db.Warehouses, syncId, ct),
            "cashbox" => ResolveAsync(_db.CashBoxes, syncId, ct),
            "bankaccount" => ResolveAsync(_db.BankAccounts, syncId, ct),
            "expensetype" => ResolveAsync(_db.ExpenseTypes, syncId, ct),
            "investor" => ResolveAsync(_db.Investors, syncId, ct),
            _ => Task.FromResult<int?>(null)
        };

    public Task<Guid?> ResolveSyncIdByIdAsync(string entityType, int id, CancellationToken ct = default) =>
        entityType.ToLowerInvariant() switch
        {
            "category" => ResolveSyncAsync(_db.Categories, id, ct),
            "product" => ResolveSyncAsync(_db.Products, id, ct),
            "customer" => ResolveSyncAsync(_db.Customers, id, ct),
            "supplier" => ResolveSyncAsync(_db.Suppliers, id, ct),
            "warehouse" => ResolveSyncAsync(_db.Warehouses, id, ct),
            "cashbox" => ResolveSyncAsync(_db.CashBoxes, id, ct),
            "bankaccount" => ResolveSyncAsync(_db.BankAccounts, id, ct),
            "expensetype" => ResolveSyncAsync(_db.ExpenseTypes, id, ct),
            "investor" => ResolveSyncAsync(_db.Investors, id, ct),
            _ => Task.FromResult<Guid?>(null)
        };

    private static async Task<int?> ResolveAsync<T>(DbSet<T> set, Guid syncId, CancellationToken ct)
        where T : Cloud.Core.Entities.CloudBaseEntity
    {
        var id = await set.AsNoTracking()
            .Where(e => e.SyncId == syncId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct);
        return id;
    }

    private static async Task<Guid?> ResolveSyncAsync<T>(DbSet<T> set, int id, CancellationToken ct)
        where T : Cloud.Core.Entities.CloudBaseEntity
    {
        var syncId = await set.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (Guid?)e.SyncId)
            .FirstOrDefaultAsync(ct);
        return syncId;
    }
}
