using FluentValidation;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem
{
    public class DeleteMenuItemResponse : IMapFrom<MenuItem>
    {
        public Guid MenuItemId { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string ImageUrl { get; set; }
        public string? Description { get; set; }

        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;

        public Station Station { get; set; }
        public int ExpectedTime { get; set; } 

        public decimal PriceDineIn { get; set; }
        public decimal PriceTakeAway { get; set; }
        public decimal CostPrice { get; set; } 

        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
