using FluentValidation;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemResponse : IMapFrom<MenuItem>
    {
        public Guid MenuItemId { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string ImageUrl { get; set; }
        public string? Description { get; set; }

        public Guid CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;

        public Station Station { get; set; }
        public int ExpectedTime { get; set; } // Minutes

        public decimal PriceDineIn { get; set; }
        public decimal PriceTakeAway { get; set; }
        public decimal? CostPrice { get; set; } // Internal cost
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedByEmployeeId { get; set; }
    }
}
