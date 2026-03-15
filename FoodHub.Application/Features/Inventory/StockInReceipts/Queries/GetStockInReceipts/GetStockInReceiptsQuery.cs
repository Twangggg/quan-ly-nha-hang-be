using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceipts
{
    public class GetStockInReceiptsQuery
        : IRequest<Result<PagedResult<GetStockInReceiptsResponse>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        public PaginationParams ToPaginationParams()
        {
            return new PaginationParams
            {
                PageNumber = PageNumber,
                PageSize = PageSize,
                Search = Search,
            };
        }
    }
}
