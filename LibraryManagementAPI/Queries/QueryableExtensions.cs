using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Queries;

public static class QueryableExtensions
{
    /// <summary>Counts all matches, then fetches one page of them. The query must already be ordered.</summary>
    public static async Task<(List<T> Items, int TotalCount)> ToPageAsync<T>(
        this IQueryable<T> query, PagingParameters paging)
    {
        var total = await query.CountAsync();
        var items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();
        return (items, total);
    }

    public static IOrderedQueryable<T> OrderBy<T, TKey>(
        this IQueryable<T> query, System.Linq.Expressions.Expression<Func<T, TKey>> key, SortDirection direction) =>
        direction == SortDirection.Desc ? query.OrderByDescending(key) : query.OrderBy(key);

    public static IOrderedQueryable<T> ThenBy<T, TKey>(
        this IOrderedQueryable<T> query, System.Linq.Expressions.Expression<Func<T, TKey>> key, SortDirection direction) =>
        direction == SortDirection.Desc ? query.ThenByDescending(key) : query.ThenBy(key);
}
