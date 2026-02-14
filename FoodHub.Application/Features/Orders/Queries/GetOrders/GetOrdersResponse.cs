using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Features.OrderItems.Common;

namespace FoodHub.Application.Features.Orders.Queries.GetOrders
{
    public class GetOrdersResponse : IMapFrom<Order>
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string OrderType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Guid? TableId { get; set; }
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPriority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public ICollection<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Order, GetOrdersResponse>()
                .ForMember(d => d.OrderType,
                    opt => opt.MapFrom(s => s.OrderType.ToString()))
                .ForMember(d => d.Status,
                    opt => opt.MapFrom(s => s.Status.ToString()));
        }
    }
}
