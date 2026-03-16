using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.ReverseStockOutReceipt
{
    public record ReverseStockOutReceiptCommand(Guid StockOutReceiptId)
        : IRequest<Result<ReverseStockOutReceiptResponse>>;
}
