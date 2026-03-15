using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.ReverseStockInReceipt
{
    public record ReverseStockInReceiptCommand(Guid StockInReceiptId)
        : IRequest<Result<ReverseStockInReceiptResponse>>;
}
