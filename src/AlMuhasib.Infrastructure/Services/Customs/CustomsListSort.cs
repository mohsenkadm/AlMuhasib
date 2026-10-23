using System.Linq.Expressions;

namespace AlMuhasib.Infrastructure.Services.Customs;

/// <summary>
/// M-04 helpers: direction-aware OrderBy for allowlisted Customs list sorts.
/// </summary>
internal static class CustomsListSort
{
    public static IOrderedQueryable<T> By<T, TKey>(
        IQueryable<T> query,
        bool descending,
        Expression<Func<T, TKey>> keySelector) =>
        descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

    public static IOrderedQueryable<T> ThenByDir<T, TKey>(
        IOrderedQueryable<T> query,
        bool descending,
        Expression<Func<T, TKey>> keySelector) =>
        descending ? query.ThenByDescending(keySelector) : query.ThenBy(keySelector);
}
