using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdResponse : IMapFrom<Category>
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Category, GetCategoryByIdResponse>()
                .ForMember(d => d.Type, opt => opt.MapFrom(s => (int)s.CategoryType));
        }
    }
}
