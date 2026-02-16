using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.OrderItems.Common
{
    public class OrderItemOptionValueDto : IMapFrom<OrderItemOptionValue>
    {
        public Guid OrderItemOptionValueId { get; set; }
        public Guid OrderItemOptionGroupId { get; set; }

        public string LabelSnapshot { get; set; } = null!;
        public decimal ExtraPriceSnapshot { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<OrderItemOptionValue, OrderItemOptionValueDto>();
        }
    }
}
