using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Billing.Commands.SplitBill
{
    public class SplitBillResponse
    {
        public Guid SourceOrderId { get; set; }
        public string SourceOrderCode { get; set; } = string.Empty;
        public decimal SourceOrderTotalAmount { get; set; }
        public List<SplitBillItemDto> SourceOrderItems { get; set; } = new();
        public Guid DestinationOrderId { get; set; }
        public string DestinationOrderCode { get; set; } = string.Empty;
        public decimal DestinationOrderTotalAmount { get; set; }
        public List<SplitBillItemDto> DestinationOrderItems { get; set; } = new();
        public Guid? DestinationTableId { get; set; }
    }

    public class SplitBillItemDto : IMapFrom<OrderItem>
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public Guid MenuItemId { get; set; }
        public string ItemNameSnapshot { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<OrderItem, SplitBillItemDto>();
        }
    }
}
