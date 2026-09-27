using AlMuhasib.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Data;

/// <summary>Defense-in-depth branch scoping for explicit queries (reports / all-branches).</summary>
public static class BranchQueryExtensions
{
    public static IQueryable<T> ForBranch<T>(this IQueryable<T> query, int branchId)
        where T : class, IBranchEntity
    {
        if (branchId <= 0)
            throw new InvalidOperationException("A valid branch id is required.");
        return query.Where(e => e.BranchId == branchId);
    }

    public static IQueryable<T> ForBranches<T>(this IQueryable<T> query, IEnumerable<int> branchIds)
        where T : class, IBranchEntity
    {
        var ids = branchIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            throw new InvalidOperationException("At least one branch id is required.");
        return query.Where(e => ids.Contains(e.BranchId));
    }

    public static IQueryable<T> ForBranchContext<T>(this IQueryable<T> query, IBranchContext context)
        where T : class, IBranchEntity
    {
        if (context.IsAllBranchesMode)
        {
            if (!context.CanViewAllBranches)
                throw new UnauthorizedAccessException("All-branches mode is not allowed.");
            return query.ForBranches(context.AllowedBranchIds);
        }

        var branchId = context.CurrentBranchId
            ?? throw new InvalidOperationException("Branch context is required.");
        return query.ForBranch(branchId);
    }
}
