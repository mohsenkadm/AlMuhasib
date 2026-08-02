using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldSupplierService : IGoldSupplierService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldSupplierService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<GoldSupplierListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = true,
        bool creditOnly = false,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldSuppliers.AsNoTracking().AsQueryable();

        if (activeOnly == true)
            query = query.Where(s => s.IsActive);
        else if (activeOnly == false)
            query = query.Where(s => !s.IsActive);

        if (creditOnly)
            query = query.Where(s => s.CreditBalanceIqd > 0 || s.CreditBalanceUsd > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Name.Contains(term) ||
                s.Phone.Contains(term) ||
                s.Address.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var supplierIds = suppliers.Select(s => s.Id).ToList();
        var invoiceStats = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.SupplierId.HasValue &&
                        supplierIds.Contains(i.SupplierId.Value) &&
                        i.Status != GoldInvoiceStatus.Cancelled)
            .GroupBy(i => i.SupplierId!.Value)
            .Select(g => new
            {
                SupplierId = g.Key,
                OpenCount = g.Count(x => x.RemainingAmount > 0),
                LastDate = g.Max(x => (DateTime?)x.InvoiceDate)
            })
            .ToDictionaryAsync(x => x.SupplierId, cancellationToken);

        var items = suppliers.Select(s =>
        {
            invoiceStats.TryGetValue(s.Id, out var stats);
            return new GoldSupplierListItem
            {
                Id = s.Id,
                Name = s.Name,
                Phone = s.Phone,
                Address = s.Address,
                CreditBalanceIqd = s.CreditBalanceIqd,
                CreditBalanceUsd = s.CreditBalanceUsd,
                IsActive = s.IsActive,
                OpenInvoiceCount = stats?.OpenCount ?? 0,
                LastTransactionDate = stats?.LastDate
            };
        }).ToList();

        return (items, totalCount);
    }

    public async Task<GoldSupplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldSuppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<GoldSupplier> CreateAsync(GoldSupplier supplier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(supplier.Name))
            throw new InvalidOperationException("اسم المورد مطلوب");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        supplier.IsActive = true;
        await context.GoldSuppliers.AddAsync(supplier, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task<GoldSupplier> UpdateAsync(GoldSupplier supplier, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.GoldSuppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id, cancellationToken)
            ?? throw new InvalidOperationException("المورد غير موجود");

        existing.Name = supplier.Name;
        existing.Phone = supplier.Phone ?? string.Empty;
        existing.Address = supplier.Address ?? string.Empty;
        existing.Notes = supplier.Notes ?? string.Empty;
        existing.IsActive = supplier.IsActive;
        existing.CreditBalanceIqd = supplier.CreditBalanceIqd;
        existing.CreditBalanceUsd = supplier.CreditBalanceUsd;

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var supplier = await context.GoldSuppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("المورد غير موجود");

        if (supplier.CreditBalanceIqd > 0 || supplier.CreditBalanceUsd > 0)
            throw new InvalidOperationException("لا يمكن حذف مورد عليه ذمم");

        supplier.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    internal static void AdjustCredit(GoldSupplier supplier, GoldCurrency currency, decimal delta)
    {
        if (currency == GoldCurrency.IQD)
            supplier.CreditBalanceIqd = GoldCurrencyHelper.Round(supplier.CreditBalanceIqd + delta);
        else
            supplier.CreditBalanceUsd = GoldCurrencyHelper.Round(supplier.CreditBalanceUsd + delta);

        if (supplier.CreditBalanceIqd < 0)
            supplier.CreditBalanceIqd = 0;
        if (supplier.CreditBalanceUsd < 0)
            supplier.CreditBalanceUsd = 0;
    }
}
