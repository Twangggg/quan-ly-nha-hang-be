using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt
{
    public class CreateStockInReceiptCommand : IRequest<Result<CreateStockInReceiptResponse>>
    {
        public DateTime? ReceivedAt { get; set; }
        public string? Note { get; set; }
        public List<CreateStockInReceiptItemDto> Items { get; set; } = new();
    }
}
