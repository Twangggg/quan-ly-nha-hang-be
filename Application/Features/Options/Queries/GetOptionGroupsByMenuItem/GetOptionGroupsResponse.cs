using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public class GetOptionGroupsResponse : IMapFrom<OptionGroup>
    {
        public Guid OptionGroupId { get; set; }
        public Guid MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public bool IsRequired { get; set; }
        public List<OptionItemResponse>? OptionItems { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<OptionGroup, GetOptionGroupsResponse>()
                .ForMember(d => d.OptionGroupId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Type, opt => opt.MapFrom(s => (int)s.Type));
        }
    }

    public class OptionItemResponse : IMapFrom<OptionItem>
    {
        public Guid OptionItemId { get; set; }
        public Guid OptionGroupId { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<OptionItem, OptionItemResponse>()
                .ForMember(d => d.OptionItemId, opt => opt.MapFrom(s => s.Id));
        }
    }
}
