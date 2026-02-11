using System.Text.Json;
using FoodHub.Application.Common.Models;

namespace FoodHub.WebAPI.Presentation.Extensions
{
    public static class PaginationExtensions
    {
        public static void AddPaginationHeaders<T>(this HttpResponse response, PagedResult<T> pagedResult)
        {
            var paginationMetadata = new
            {
                pagedResult.TotalCount,
                pagedResult.PageSize,
                pagedResult.PageNumber,
                pagedResult.TotalPages,
                pagedResult.HasPrevious,
                pagedResult.HasNext
            };
            response.Headers.Append("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
            response.Headers.Append("Access-Control-Expose-Headers", "X-Pagination");
        }
    }
}
