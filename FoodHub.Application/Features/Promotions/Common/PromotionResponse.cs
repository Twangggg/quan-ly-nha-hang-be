using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Promotions.Common
{
    public class PromotionResponse : IMapFrom<Promotion>
    {
        public Guid PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Type { get; set; }
        public decimal Value { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public Guid? ItemId { get; set; }
        public string? ItemName { get; set; }
        public int? FreeQuantity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile
                .CreateMap<Promotion, PromotionResponse>()
                .ForMember(d => d.Type, opt => opt.MapFrom(s => (int)s.Type))
                .ForMember(d => d.ItemName, opt => opt.MapFrom(s => s.Item != null ? s.Item.Name : null));
        }
    }
}
