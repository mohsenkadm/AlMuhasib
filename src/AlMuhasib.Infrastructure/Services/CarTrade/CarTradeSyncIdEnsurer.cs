using AlMuhasib.Infrastructure.Data.CarTrade;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.CarTrade;

internal static class CarTradeSyncIdEnsurer
{
    public static async Task EnsureAllAsync(CarTradeDbContext db, CancellationToken ct)
    {
        foreach (var transaction in await db.CarTradeTransactions.IgnoreQueryFilters()
                     .Where(t => t.SyncId == Guid.Empty).ToListAsync(ct))
        {
            transaction.SyncId = Guid.NewGuid();
        }

        foreach (var payment in await db.CarTradePayments.IgnoreQueryFilters()
                     .Where(p => p.SyncId == Guid.Empty).ToListAsync(ct))
        {
            payment.SyncId = Guid.NewGuid();
        }

        await db.SaveChangesAsync(ct);
    }
}
