using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsQueue
{
    public class KdsQueueResponse : IMapFrom<OrderItem>
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string ItemNameSnapshot { get; set; } = null!;
        public string StationSnapshot { get; set; } = null!;
        public int Quantity { get; set; }
        public string? ItemNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PriorityScore { get; set; }
        public int QueuePosition { get; set; }

        public void Mapping(Profile profile)
        {
            profile
                .CreateMap<OrderItem, KdsQueueResponse>()
                .ForMember(d => d.OrderCode, opt => opt.MapFrom(s => s.Order.OrderCode))
                .ForMember(d => d.ItemNote, opt => opt.MapFrom(s => s.ItemNote));
        }
    }
}
