using AlMuhasib.Core.Entities;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>
/// يضمن أن كل سجل له SyncId فريد قبل المزامنة (السجلات القديمة قد تكون 00000000-...).
/// </summary>
internal static class SyncIdEnsurer
{
    public static async Task EnsureAllAsync(AppDbContext db, CancellationToken ct = default)
    {
        var changed = false;
        changed |= await EnsureAsync(db.Categories, ct);
        changed |= await EnsureAsync(db.Products, ct);
        changed |= await EnsureAsync(db.Customers, ct);
        changed |= await EnsureAsync(db.Suppliers, ct);
        changed |= await EnsureAsync(db.Warehouses, ct);
        changed |= await EnsureAsync(db.WarehouseStocks, ct);
        changed |= await EnsureAsync(db.CashBoxes, ct);
        changed |= await EnsureAsync(db.BankAccounts, ct);
        changed |= await EnsureAsync(db.Investors, ct);
        changed |= await EnsureAsync(db.ExpenseTypes, ct);
        changed |= await EnsureAsync(db.Invoices, ct);
        changed |= await EnsureAsync(db.InvoiceItems, ct);
        changed |= await EnsureAsync(db.InstallmentPlans, ct);
        changed |= await EnsureAsync(db.Installments, ct);
        changed |= await EnsureAsync(db.Vouchers, ct);
        changed |= await EnsureAsync(db.Expenses, ct);
        changed |= await EnsureAsync(db.Transfers, ct);
        changed |= await EnsureAsync(db.InvestorTransactions, ct);
        changed |= await EnsureAsync(db.ProfitDistributions, ct);
        changed |= await EnsureAsync(db.ProfitDistributionDetails, ct);
        changed |= await EnsureAsync(db.CapitalEntries, ct);
        changed |= await EnsureAsync(db.CustomerAttachments, ct);
        changed |= await EnsureAsync(db.PrintBrandingSettings, ct);

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
