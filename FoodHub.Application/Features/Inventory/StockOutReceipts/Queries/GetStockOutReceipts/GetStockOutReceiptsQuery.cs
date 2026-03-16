using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceipts
{
    public class GetStockOutReceiptsQuery
        : IRequest<Result<PagedResult<GetStockOutReceiptsResponse>>>
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
