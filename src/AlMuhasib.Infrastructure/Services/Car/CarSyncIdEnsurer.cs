using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.Car;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Car;

internal static class CarSyncIdEnsurer
{
    public static async Task EnsureAllAsync(CarDbContext db, CancellationToken ct)
    {
        foreach (var contract in await db.CarSaleContracts.IgnoreQueryFilters()
                     .Where(c => c.SyncId == Guid.Empty).ToListAsync(ct))
        {
            contract.SyncId = Guid.NewGuid();
        }

        foreach (var payment in await db.CarContractPayments.IgnoreQueryFilters()
                     .Where(p => p.SyncId == Guid.Empty).ToListAsync(ct))
        {
            payment.SyncId = Guid.NewGuid();
        }

        await db.SaveChangesAsync(ct);
    }
}
