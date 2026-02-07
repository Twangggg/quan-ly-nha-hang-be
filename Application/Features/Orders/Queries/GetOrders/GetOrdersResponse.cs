using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Features.Employees.Queries.GetEmployees;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

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
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
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
