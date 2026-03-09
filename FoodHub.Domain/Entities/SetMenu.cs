using System;
using System.Collections.Generic;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class SetMenu : BaseEntity
    {
        public Guid SetMenuId { get; set; }
        public required string Code { get; set; }
        public int ItemNumber { get; set; }
        public required string Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public bool IsOutOfStock { get; set; }
        public Guid? CategoryId { get; set; }
        public virtual Category? Category { get; set; }
        public virtual ICollection<SetMenuItem> SetMenuItems { get; set; } = new List<SetMenuItem>();

        public bool CanDelete()
        {
            return !SetMenuItems.Any();
        }

        public DomainResult SoftDelete()
        {
            if (!CanDelete())
            {
                return DomainResult.Failure(DomainErrors.SetMenu.CannotDeleteWithItems);
            }

            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult MarkOutOfStock()
        {
            IsOutOfStock = true;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult MarkInStock()
        {
            IsOutOfStock = false;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult ValidatePrice()
        {
            if (Price <= 0)
            {
                return DomainResult.Failure(DomainErrors.SetMenu.InvalidPrice);
            }
            return DomainResult.Success();
        }

        public decimal GetProfitMargin()
        {
            if (Price <= 0) return 0;
            return ((Price - CostPrice) / Price) * 100;
        }

        public decimal GetProfitAmount()
        {
            return Price - CostPrice;
        }

        public int GetTotalItemsCount()
        {
            return SetMenuItems.Count;
        }
    }
}
