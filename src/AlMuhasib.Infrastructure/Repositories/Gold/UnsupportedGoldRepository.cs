using System.Linq.Expressions;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;

namespace AlMuhasib.Infrastructure.Repositories.Gold;

internal sealed class UnsupportedGoldRepository<T> : IRepository<T> where T : BaseEntity
{
    private static NotSupportedException Error =>
        new("This data module is not available in the gold shop system.");

    public Task<T?> GetByIdAsync(int id) => throw Error;
    public Task<IEnumerable<T>> GetAllAsync() => throw Error;
    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => throw Error;
    public Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null) => throw Error;
    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) => throw Error;
    public Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null) => throw Error;
    public Task AddAsync(T entity) => throw Error;
    public Task AddRangeAsync(IEnumerable<T> entities) => throw Error;
    public void Update(T entity) => throw Error;
    public void SoftDelete(T entity, string deletedBy) => throw Error;
    public Task<T?> FindSoftDeletedFirstAsync(Expression<Func<T, bool>> predicate) => throw Error;
    public IQueryable<T> Query() => throw Error;
}
