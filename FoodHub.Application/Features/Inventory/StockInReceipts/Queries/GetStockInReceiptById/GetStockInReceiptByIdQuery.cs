using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceiptById
{
    public record GetStockInReceiptByIdQuery(Guid StockInReceiptId)
        : IRequest<Result<GetStockInReceiptByIdResponse>>;
}
