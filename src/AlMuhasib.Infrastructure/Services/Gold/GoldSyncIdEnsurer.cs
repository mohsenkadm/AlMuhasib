using AlMuhasib.Core.Entities;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

internal static class GoldSyncIdEnsurer
{
    public static async Task EnsureAllAsync(GoldDbContext db, CancellationToken ct)
    {
        await EnsureAsync(db.GoldSettings, ct);
        await EnsureAsync(db.GoldFxRates, ct);
        await EnsureAsync(db.GoldKarats, ct);
        await EnsureAsync(db.GoldMithqalPrices, ct);
        await EnsureAsync(db.GoldItems, ct);
        await EnsureAsync(db.GoldStockBalances, ct);
        await EnsureAsync(db.GoldCustomers, ct);
        await EnsureAsync(db.GoldCashBoxes, ct);
        await EnsureAsync(db.GoldInvoices, ct);
        await EnsureAsync(db.GoldInvoiceLines, ct);
        await EnsureAsync(db.GoldPayments, ct);
        await EnsureAsync(db.GoldVouchers, ct);
        await EnsureAsync(db.GoldNotifications, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureAsync<T>(DbSet<T> set, CancellationToken ct) where T : BaseEntity
    {
        foreach (var entity in await set.IgnoreQueryFilters()
                     .Where(e => e.SyncId == Guid.Empty).ToListAsync(ct))
        {
            entity.SyncId = Guid.NewGuid();
        }
    }
}
