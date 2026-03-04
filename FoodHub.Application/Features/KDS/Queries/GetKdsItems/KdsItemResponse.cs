using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsItems
{
    public class KdsItemResponse : IMapFrom<OrderItem>
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string ItemNameSnapshot { get; set; } = null!;
        public string StationSnapshot { get; set; } = null!;
        public int Quantity { get; set; }
        public string? ItemNote { get; set; }
        public string Status { get; set; } = null!;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PriorityScore { get; set; }
        public string? ItemOptions { get; set; }
        public bool IsOrderPriority { get; set; }
        public string OrderType { get; set; } = null!;
        public int TotalOrderItems { get; set; }
        public int FinishedOrderItems { get; set; }
        public int ExpectedTimeSeconds { get; set; }
        public ICollection<OrderItemOptionGroup> OptionGroups { get; set; } =
            new List<OrderItemOptionGroup>();

        public void Mapping(Profile profile)
        {
            profile
                .CreateMap<OrderItem, KdsItemResponse>()
                .ForMember(d => d.OrderCode, opt => opt.MapFrom(s => s.Order.OrderCode))
                .ForMember(d => d.ItemNote, opt => opt.MapFrom(s => s.ItemNote))
                .ForMember(
                    d => d.ItemOptions,
                    opt =>
                        opt.MapFrom(s =>
                            string.Join(
                                ", ",
                                s.OptionGroups.SelectMany(g => g.OptionValues)
                                    .Select(v =>
                                        v.Quantity > 1
                                            ? $"{v.LabelSnapshot} x{v.Quantity}"
                                            : v.LabelSnapshot
                                    )
                            )
                        )
                );
        }
    }
}
