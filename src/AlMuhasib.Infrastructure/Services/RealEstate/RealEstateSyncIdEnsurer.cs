using AlMuhasib.Core.Entities;
using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.RealEstate;

internal static class RealEstateSyncIdEnsurer
{
    public static async Task EnsureAllAsync(RealEstateDbContext db, CancellationToken ct)
    {
        await EnsureAsync(db.RealEstateContracts, ct);
        await EnsureAsync(db.RealEstateContractPayments, ct);
        await EnsureAsync(db.RealEstateContractClauses, ct);
        await EnsureAsync(db.RealEstateClauseTemplates, ct);
        await EnsureAsync(db.RealEstateParties, ct);
        await EnsureAsync(db.RealEstateExpenseTypes, ct);
        await EnsureAsync(db.RealEstateExpenses, ct);
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
