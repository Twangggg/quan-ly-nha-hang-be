using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Extensions.Pagination
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var pagination = new PaginationParams
            {
                PageIndex = pageNumber,
                PageSize = pageSize
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>(items, pagination, totalCount);
        }
    }
}
