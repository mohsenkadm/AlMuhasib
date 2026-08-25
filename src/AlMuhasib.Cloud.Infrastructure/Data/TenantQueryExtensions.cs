using AlMuhasib.Cloud.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Data;

/// <summary>
/// Defense-in-depth tenant scoping. Prefer this in addition to the global EF filter.
/// </summary>
public static class TenantQueryExtensions
{
    public static IQueryable<T> ForTenant<T>(this IQueryable<T> query, int tenantId)
        where T : CloudBaseEntity
    {
        if (tenantId <= 0)
            throw new InvalidOperationException("A valid tenant id is required for data access.");

        return query.Where(e => e.TenantId == tenantId);
    }

    public static IQueryable<T> ForTenantTracked<T>(this DbSet<T> set, int tenantId)
        where T : CloudBaseEntity
        => set.ForTenant(tenantId);
}
