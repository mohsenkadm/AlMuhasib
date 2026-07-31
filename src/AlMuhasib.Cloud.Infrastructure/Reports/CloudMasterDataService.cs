using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Sync;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public sealed class CloudMasterDataService : ICloudMasterDataService
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CloudMasterDataService(CloudDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<MasterDataBundle> GetAllAsync(CancellationToken ct = default) => new()
    {
        Categories = await GetCategoriesAsync(ct: ct),
        Products = await GetProductsAsync(ct: ct),
        PricingTypes = await GetPricingTypesAsync(ct: ct),
        Customers = await GetCustomersAsync(ct: ct),
        Suppliers = await GetSuppliersAsync(ct: ct),
        Warehouses = await GetWarehousesAsync(ct: ct),
        CashBoxes = await GetCashBoxesAsync(ct: ct),
        BankAccounts = await GetBankAccountsAsync(ct: ct),
        ExpenseTypes = await GetExpenseTypesAsync(ct: ct),
        Investors = await GetInvestorsAsync(ct: ct),
        BusinessSettings = await GetBusinessSettingsAsync(ct)
    };

    public Task<List<LookupItem>> GetCategoriesAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.Name, term));
        }

        return query.OrderBy(c => c.Name)
            .Select(c => new LookupItem { Id = c.Id, SyncId = c.SyncId, Name = c.Name })
            .ToListAsync(ct);
    }

    public async Task<List<ProductLookupItem>> GetProductsAsync(
        string? search = null, Guid? categorySyncId = null, string? barcode = null, CancellationToken ct = default)
    {
        var query = _db.Products.AsNoTracking().AsQueryable();
        if (categorySyncId.HasValue)
            query = query.Where(p => p.Category.SyncId == categorySyncId.Value);
        if (!string.IsNullOrWhiteSpace(barcode))
            query = query.Where(p => p.Barcode == barcode.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, term) ||
                (p.Barcode != null && EF.Functions.Like(p.Barcode, term)) ||
                (p.ScientificName != null && EF.Functions.Like(p.ScientificName, term)));
        }

        var products = await query.OrderBy(p => p.Name)
            .Select(p => new ProductLookupItem
            {
                Id = p.Id,
                SyncId = p.SyncId,
                Name = p.Name,
                Barcode = p.Barcode,
                ScientificName = p.ScientificName,
                CategorySyncId = p.Category.SyncId,
                CategoryName = p.Category.Name
            })
            .ToListAsync(ct);

        if (products.Count == 0)
            return products;

        var productIds = products.Select(p => p.Id).ToHashSet();
        var prices = await _db.ProductPrices.AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId))
            .Select(p => new
            {
                p.ProductId,
                Item = new ProductPriceLookupItem
                {
                    SyncId = p.SyncId,
                    ProductSyncId = p.Product.SyncId,
                    ProductName = p.Product.Name,
                    PricingTypeSyncId = p.PricingType.SyncId,
                    PricingTypeName = p.PricingType.Name,
                    IsDefaultPricingType = p.PricingType.IsDefault,
                    SalePrice = p.SalePrice,
                    PurchasePrice = p.PurchasePrice
                }
            })
            .ToListAsync(ct);

        var byProduct = prices.GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Item).OrderByDescending(x => x.IsDefaultPricingType).ThenBy(x => x.PricingTypeName).ToList());

        foreach (var product in products)
            product.Prices = byProduct.GetValueOrDefault(product.Id) ?? [];

        return products;
    }

    public async Task<List<PricingTypeLookupItem>> GetPricingTypesAsync(string? search = null, CancellationToken ct = default)
    {
        await EnsureDefaultPricingDataAsync(ct);

        var query = _db.PricingTypes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(t => EF.Functions.Like(t.Name, term));
        }

        return await query.OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name)
            .Select(t => new PricingTypeLookupItem
            {
                Id = t.Id,
                SyncId = t.SyncId,
                Name = t.Name,
                IsDefault = t.IsDefault,
                IsActive = t.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<List<ProductPriceLookupItem>> GetProductPricesAsync(
        Guid? productSyncId = null, Guid? pricingTypeSyncId = null, CancellationToken ct = default)
    {
        var query = _db.ProductPrices.AsNoTracking().AsQueryable();
        if (productSyncId.HasValue)
            query = query.Where(p => p.Product.SyncId == productSyncId.Value);
        if (pricingTypeSyncId.HasValue)
            query = query.Where(p => p.PricingType.SyncId == pricingTypeSyncId.Value);

        return await query
            .OrderBy(p => p.Product.Name)
            .ThenByDescending(p => p.PricingType.IsDefault)
            .ThenBy(p => p.PricingType.Name)
            .Select(p => new ProductPriceLookupItem
            {
                SyncId = p.SyncId,
                ProductSyncId = p.Product.SyncId,
                ProductName = p.Product.Name,
                PricingTypeSyncId = p.PricingType.SyncId,
                PricingTypeName = p.PricingType.Name,
                IsDefaultPricingType = p.PricingType.IsDefault,
                SalePrice = p.SalePrice,
                PurchasePrice = p.PurchasePrice
            })
            .ToListAsync(ct);
    }

    public async Task<BusinessSettingsDto> GetBusinessSettingsAsync(CancellationToken ct = default)
    {
        var settings = await EnsureBusinessSettingsAsync(ct);
        return new BusinessSettingsDto
        {
            SyncId = settings.SyncId,
            ProductPricingEnabled = settings.ProductPricingEnabled,
            UpdateProductPriceOnPurchase = settings.UpdateProductPriceOnPurchase
        };
    }

    public async Task<List<LookupItem>> GetCustomersAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Name, term) ||
                (c.Phone != null && EF.Functions.Like(c.Phone, term)));
        }

        var customers = await query.OrderBy(c => c.Name)
            .Select(c => new LookupItem { Id = c.Id, SyncId = c.SyncId, Name = c.Name, Extra = c.Phone })
            .ToListAsync(ct);

        if (customers.Count == 0)
            return customers;

        var customerIds = customers.Select(c => c.Id).ToList();

        var creditByCustomer = await _db.Invoices.AsNoTracking()
            .Where(i => i.CustomerId != null && customerIds.Contains(i.CustomerId.Value) &&
                        i.PaymentMethod == PaymentMethod.Credit)
            .GroupBy(i => i.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Remaining = g.Sum(i => i.RemainingAmount) })
            .ToListAsync(ct);

        var planRows = await _db.InstallmentPlans.AsNoTracking()
            .Where(p => customerIds.Contains(p.CustomerId))
            .Select(p => new { p.Id, p.CustomerId })
            .ToListAsync(ct);
        var planIds = planRows.Select(p => p.Id).ToList();
        var installmentByPlan = planIds.Count == 0
            ? []
            : await _db.Installments.AsNoTracking()
                .Where(i => planIds.Contains(i.InstallmentPlanId) && i.Status != InstallmentStatus.Paid)
                .GroupBy(i => i.InstallmentPlanId)
                .Select(g => new { PlanId = g.Key, Remaining = g.Sum(i => i.RemainingAmount) })
                .ToListAsync(ct);

        var installmentByCustomer = planRows
            .GroupBy(p => p.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(p => installmentByPlan.FirstOrDefault(x => x.PlanId == p.Id)?.Remaining ?? 0));

        var unappliedDebt = await _db.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId != null && customerIds.Contains(v.CustomerId.Value) &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .GroupBy(v => v.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(v => v.Amount) })
            .ToListAsync(ct);

        var receipts = await _db.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId != null && customerIds.Contains(v.CustomerId.Value) &&
                        v.VoucherType == VoucherType.Receipt)
            .GroupBy(v => v.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(v => v.Amount) })
            .ToListAsync(ct);

        foreach (var customer in customers)
        {
            var credit = creditByCustomer.FirstOrDefault(x => x.CustomerId == customer.Id)?.Remaining ?? 0;
            installmentByCustomer.TryGetValue(customer.Id, out var inst);
            var debt = unappliedDebt.FirstOrDefault(x => x.CustomerId == customer.Id)?.Amount ?? 0;
            var receipt = receipts.FirstOrDefault(x => x.CustomerId == customer.Id)?.Amount ?? 0;
            customer.Balance = CustomerBalanceHelper.ComputeOutstandingBalance(credit, inst, debt, receipt);
        }

        return customers;
    }

    public Task<List<LookupItem>> GetSuppliersAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s =>
                EF.Functions.Like(s.Name, term) ||
                (s.Phone != null && EF.Functions.Like(s.Phone, term)));
        }

        return query.OrderBy(s => s.Name)
            .Select(s => new LookupItem { Id = s.Id, SyncId = s.SyncId, Name = s.Name, Extra = s.Phone })
            .ToListAsync(ct);
    }

    public Task<List<LookupItem>> GetWarehousesAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Warehouses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(w => EF.Functions.Like(w.Name, term));
        }

        return query.OrderBy(w => w.Name)
            .Select(w => new LookupItem { Id = w.Id, SyncId = w.SyncId, Name = w.Name })
            .ToListAsync(ct);
    }

    public Task<List<LookupItem>> GetCashBoxesAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.CashBoxes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.Name, term));
        }

        return query.OrderBy(c => c.Name)
            .Select(c => new LookupItem { Id = c.Id, SyncId = c.SyncId, Name = c.Name })
            .ToListAsync(ct);
    }

    public Task<List<LookupItem>> GetBankAccountsAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.BankAccounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(b => EF.Functions.Like(b.Name, term));
        }

        return query.OrderBy(b => b.Name)
            .Select(b => new LookupItem { Id = b.Id, SyncId = b.SyncId, Name = b.Name })
            .ToListAsync(ct);
    }

    public Task<List<LookupItem>> GetExpenseTypesAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.ExpenseTypes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(e => EF.Functions.Like(e.Name, term));
        }

        return query.OrderBy(e => e.Name)
            .Select(e => new LookupItem { Id = e.Id, SyncId = e.SyncId, Name = e.Name })
            .ToListAsync(ct);
    }

    public Task<List<LookupItem>> GetInvestorsAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Investors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.Like(i.Name, term) ||
                (i.Phone != null && EF.Functions.Like(i.Phone, term)));
        }

        return query.OrderBy(i => i.Name)
            .Select(i => new LookupItem { Id = i.Id, SyncId = i.SyncId, Name = i.Name })
            .ToListAsync(ct);
    }

    public Task<int?> ResolveIdBySyncIdAsync(string entityType, Guid syncId, CancellationToken ct = default) =>
        entityType.ToLowerInvariant() switch
        {
            "category" => ResolveAsync(_db.Categories, syncId, ct),
            "product" => ResolveAsync(_db.Products, syncId, ct),
            "pricingtype" => ResolveAsync(_db.PricingTypes, syncId, ct),
            "productprice" => ResolveAsync(_db.ProductPrices, syncId, ct),
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
            "pricingtype" => ResolveSyncAsync(_db.PricingTypes, id, ct),
            "productprice" => ResolveSyncAsync(_db.ProductPrices, id, ct),
            "customer" => ResolveSyncAsync(_db.Customers, id, ct),
            "supplier" => ResolveSyncAsync(_db.Suppliers, id, ct),
            "warehouse" => ResolveSyncAsync(_db.Warehouses, id, ct),
            "cashbox" => ResolveSyncAsync(_db.CashBoxes, id, ct),
            "bankaccount" => ResolveSyncAsync(_db.BankAccounts, id, ct),
            "expensetype" => ResolveSyncAsync(_db.ExpenseTypes, id, ct),
            "investor" => ResolveSyncAsync(_db.Investors, id, ct),
            _ => Task.FromResult<Guid?>(null)
        };

    private async Task EnsureDefaultPricingDataAsync(CancellationToken ct)
    {
        await EnsureBusinessSettingsAsync(ct);

        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required");

        var hasDefault = await _db.PricingTypes.IgnoreQueryFilters()
            .AnyAsync(t => t.TenantId == tenantId && !t.IsDeleted && t.IsDefault, ct);
        if (hasDefault)
            return;

        var bySync = await _db.PricingTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && !t.IsDeleted && t.SyncId == ProductPricingSyncIds.DefaultPricingType, ct);
        if (bySync is not null)
        {
            bySync.IsDefault = true;
            bySync.IsActive = true;
            await _db.SaveChangesAsync(ct);
            return;
        }

        var byName = await _db.PricingTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && !t.IsDeleted && t.Name == "سعر مفرد", ct);
        if (byName is not null)
        {
            byName.IsDefault = true;
            byName.IsActive = true;
            await _db.SaveChangesAsync(ct);
            return;
        }

        _db.PricingTypes.Add(new CloudPricingType
        {
            TenantId = tenantId,
            SyncId = ProductPricingSyncIds.DefaultPricingType,
            Name = "سعر مفرد",
            IsDefault = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CloudBusinessSettings> EnsureBusinessSettingsAsync(CancellationToken ct)
    {
        var existing = await _db.BusinessSettings
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.TenantId == _tenantContext.TenantId)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            return existing;

        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required");

        var settings = new CloudBusinessSettings
        {
            TenantId = tenantId,
            SyncId = ProductPricingSyncIds.BusinessSettings,
            ProductPricingEnabled = false,
            UpdateProductPriceOnPurchase = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };
        _db.BusinessSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }

    private static async Task<int?> ResolveAsync<T>(DbSet<T> set, Guid syncId, CancellationToken ct)
        where T : CloudBaseEntity
    {
        var id = await set.AsNoTracking()
            .Where(e => e.SyncId == syncId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct);
        return id;
    }

    private static async Task<Guid?> ResolveSyncAsync<T>(DbSet<T> set, int id, CancellationToken ct)
        where T : CloudBaseEntity
    {
        var syncId = await set.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (Guid?)e.SyncId)
            .FirstOrDefaultAsync(ct);
        return syncId;
    }
}
