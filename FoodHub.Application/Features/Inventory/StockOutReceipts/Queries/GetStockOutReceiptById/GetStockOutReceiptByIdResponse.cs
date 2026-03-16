using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceiptById
{
    public record GetStockOutReceiptByIdQuery(Guid StockOutReceiptId)
        : IRequest<Result<GetStockOutReceiptByIdResponse>>;

    public class GetStockOutReceiptByIdResponse
    {
        public Guid StockOutReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime StockOutDate { get; set; }
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CreatedByName { get; set; }
        public List<GetStockOutReceiptByIdItemResponse> Items { get; set; } = new();
    }

    public class GetStockOutReceiptByIdItemResponse
    {
        public Guid StockOutReceiptItemId { get; set; }
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal LineAmount { get; set; }
    }
}
