using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.CreateStockOutReceipt
{
    public class CreateStockOutReceiptCommand : IRequest<Result<CreateStockOutReceiptResponse>>
    {
        public DateTime StockOutDate { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
        public List<CreateStockOutReceiptItemDto> Items { get; set; } = new();
    }
}
