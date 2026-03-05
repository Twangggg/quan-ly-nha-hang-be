using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Areas.Queries.GetAllAreas
{
    /// <summary>
    /// Query object để lấy danh sách khu vực kèm phân trang.
    /// </summary>
    /// <param name="Pagination">Tham số phân trang, lọc và sắp xếp.</param>
    public record GetAllAreasQuery(PaginationParams Pagination)
        : IRequest<Result<PagedResult<GetAllAreasResponse>>>;
}
