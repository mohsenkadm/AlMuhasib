using System.Linq.Expressions;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly Func<AppDbContext?> _getActiveContext;
    private readonly IBranchContext? _branchContext;

    public Repository(
        IDbContextFactory<AppDbContext> contextFactory,
        Func<AppDbContext?> getActiveContext,
        IBranchContext? branchContext = null)
    {
        _contextFactory = contextFactory;
        _getActiveContext = getActiveContext;
        _branchContext = branchContext;
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        // Never use FindAsync — it bypasses soft-delete and BranchId query filters (IDOR).
        var active = _getActiveContext();
        if (active is not null)
            return await active.Set<T>().FirstOrDefaultAsync(e => e.Id == id);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var active = _getActiveContext();
        if (active is not null)
            return await active.Set<T>().ToListAsync();

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<T>().ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var active = _getActiveContext();
        if (active is not null)
            return await active.Set<T>().Where(predicate).ToListAsync();

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        var active = _getActiveContext();
        var ownsContext = active is null;
        var context = active ?? await _contextFactory.CreateDbContextAsync();
        try
        {
            IQueryable<T> query = context.Set<T>();

            if (filter is not null)
                query = query.Where(filter);

            var totalCount = await query.CountAsync();

            if (orderBy is not null)
                query = orderBy(query);
            else
                query = query.OrderByDescending(e => e.Id);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        finally
        {
            if (ownsContext) await context.DisposeAsync();
        }
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var active = _getActiveContext();
        if (active is not null)
            return predicate is null
                ? await active.Set<T>().CountAsync()
                : await active.Set<T>().CountAsync(predicate);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return predicate is null
            ? await context.Set<T>().CountAsync()
            : await context.Set<T>().CountAsync(predicate);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var active = _getActiveContext();
        if (active is not null)
            return predicate is null
                ? await active.Set<T>().AnyAsync()
                : await active.Set<T>().AnyAsync(predicate);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return predicate is null
            ? await context.Set<T>().AnyAsync()
            : await context.Set<T>().AnyAsync(predicate);
    }

    public async Task AddAsync(T entity)
    {
        var active = _getActiveContext();
        if (active is not null)
        {
            await active.Set<T>().AddAsync(entity);
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        var active = _getActiveContext();
        if (active is not null)
        {
            await active.Set<T>().AddRangeAsync(entities);
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Set<T>().AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }

    public void Update(T entity)
    {
        var active = _getActiveContext();
        if (active is not null)
        {
            active.Set<T>().Update(entity);
            return;
        }

        // لا تستخدم SaveChanges() المتزامن — يسبب تجمّد الواجهة عبر sync-over-async.
        using var context = _contextFactory.CreateDbContext();
        context.Set<T>().Update(entity);
        context.SaveChangesAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public void SoftDelete(T entity, string deletedBy)
    {
        entity.MarkSoftDeleted(deletedBy);

        var active = _getActiveContext();
        if (active is not null)
        {
            active.Entry(entity).State = EntityState.Modified;
            return;
        }

        using var context = _contextFactory.CreateDbContext();
        context.Set<T>().Attach(entity);
        context.Entry(entity).State = EntityState.Modified;
        context.SaveChangesAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<T?> FindSoftDeletedFirstAsync(Expression<Func<T, bool>> predicate)
    {
        var active = _getActiveContext();
        if (active is not null)
            return await FindSoftDeletedCoreAsync(active, predicate);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await FindSoftDeletedCoreAsync(context, predicate);
    }

    private async Task<T?> FindSoftDeletedCoreAsync(AppDbContext context, Expression<Func<T, bool>> predicate)
    {
        IQueryable<T> query = context.Set<T>()
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted);

        // Soft-delete revive must stay inside the current write branch.
        if (typeof(IBranchEntity).IsAssignableFrom(typeof(T)))
        {
            if (_branchContext?.HasWriteBranchContext == true)
            {
                var branchId = _branchContext.CurrentBranchId!.Value;
                query = query.Where(e => EF.Property<int>(e, nameof(IBranchEntity.BranchId)) == branchId);
            }
            else if (_branchContext?.AllowedBranchIds.Count > 0)
            {
                var allowed = _branchContext.AllowedBranchIds.ToList();
                query = query.Where(e => allowed.Contains(EF.Property<int>(e, nameof(IBranchEntity.BranchId))));
            }
            else
            {
                return null;
            }
        }

        return await query
            .Where(predicate)
            .OrderByDescending(e => e.DeletedAt)
            .FirstOrDefaultAsync();
    }

    public IQueryable<T> Query()
    {
        var active = _getActiveContext();
        if (active is not null)
            return active.Set<T>().AsQueryable();

        var context = _contextFactory.CreateDbContext();
        return context.Set<T>().AsQueryable();
    }
}
