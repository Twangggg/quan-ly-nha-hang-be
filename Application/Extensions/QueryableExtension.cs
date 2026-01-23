using FoodHub.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FoodHub.Application.Extensions
{
    public static class QueryableExtension
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            PaginationParams pagination)
        {
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<T>(items, pagination, totalCount);
        }

        public static IQueryable<T> ApplySorting<T>(
            this IQueryable<T> query,
            string? sortBy,
            bool isDescending,
            Dictionary<string, Expression<Func<T, object>>> mapping,
            Expression<Func<T, object>> defaultSort)
        {
            if (string.IsNullOrEmpty(sortBy) || !mapping.ContainsKey(sortBy.ToLower()))
            {
                return isDescending
                    ? query.OrderByDescending(defaultSort)
                    : query.OrderBy(defaultSort);
            }
            var selectedSort = mapping[sortBy.ToLower()];
            return isDescending
                ? query.OrderByDescending(selectedSort)
                : query.OrderBy(selectedSort);
        }
    }
}
