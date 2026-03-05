using System;
using System.Collections.Generic;
using System.Linq;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;


namespace FoodHub.Domain.Entities
{
    public class Category : BaseEntity
    {
        public Guid CategoryId { get; set; }
        public required string Name { get; set; }
        public CategoryType CategoryType { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

        public bool CanDelete()
        {
            return !MenuItems.Any();
        }

        public bool CanDeactivate()
        {
            return !MenuItems.Any(mi => !mi.IsOutOfStock);
        }

        public DomainResult SoftDelete()
        {
            if (!CanDelete())
            {
                return DomainResult.Failure(DomainErrors.Category.CannotDeleteActiveCategory);
            }

            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult Deactivate()
        {
            if (!CanDeactivate())
            {
                return DomainResult.Failure(DomainErrors.Category.CannotDeactivateWithActiveItems);
            }

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public int GetActiveMenuItemsCount()
        {
            return MenuItems.Count(mi => !mi.IsOutOfStock);
        }
    }
}
