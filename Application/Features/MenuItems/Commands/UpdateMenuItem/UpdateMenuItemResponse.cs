using FluentValidation;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemResponse : IMapFrom<MenuItem>
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string ImageUrl { get; set; } = default!;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public Station Station { get; set; }
        public int ExpectedTime { get; set; }
        public decimal PriceDineIn { get; set; }
        public decimal PriceTakeAway { get; set; }
    }
}
