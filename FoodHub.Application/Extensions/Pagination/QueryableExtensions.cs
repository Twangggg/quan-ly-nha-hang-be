using FoodHub.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Extensions.Pagination
{
    /// <summary>
    /// Các phương thức mở rộng (Extension Methods) cho IQueryable để hỗ trợ phân trang
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Thực thi phân trang trên một IQueryable dựa trên đối tượng PaginationParams
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            PaginationParams pagination,
            CancellationToken cancellationToken = default)
        {
            return await query.ToPagedResultAsync(pagination.PageNumber, pagination.PageSize, cancellationToken);
        }

        /// <summary>
        /// Đếm tổng số bản ghi và lấy dữ liệu theo trang
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var pagination = new PaginationParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 1. Đếm tổng số bản ghi thỏa mãn điều kiện (Search/Filter) trước khi phân trang
            var totalCount = await query.CountAsync(cancellationToken);
            
            // 2. Lấy dữ liệu của trang hiện tại bằng cách Skip (bỏ qua các trang trước) và Take (lấy đủ số lượng của trang này)
            // Ví dụ: Trang 2, cỡ 10 bản ghi => Skip = (2-1)*10 = 10 bản ghi đầu, Take = 10 bản ghi tiếp theo.
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>(items, pagination, totalCount);
        }
    }
}
