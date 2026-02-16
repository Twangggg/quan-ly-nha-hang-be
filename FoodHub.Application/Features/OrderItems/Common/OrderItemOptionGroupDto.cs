using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.OrderItems.Common
{
    public class OrderItemOptionGroupDto : IMapFrom<OrderItemOptionGroup>
    {
        public Guid OrderItemOptionGroupId { get; set; }
        public Guid OrderItemId { get; set; }

        public string GroupNameSnapshot { get; set; } = null!;
        public string GroupTypeSnapshot { get; set; } = null!;
        public bool IsRequiredSnapshot { get; set; }

        public ICollection<OrderItemOptionValueDto> OptionValues { get; set; } =
            new List<OrderItemOptionValueDto>();

        public void Mapping(Profile profile)
        {
            profile.CreateMap<OrderItemOptionGroup, OrderItemOptionGroupDto>();
        }
    }
}
