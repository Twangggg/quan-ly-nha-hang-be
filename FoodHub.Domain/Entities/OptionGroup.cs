using System;
using System.Collections.Generic;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class OptionGroup : BaseEntity
    {
        public Guid OptionGroupId { get; set; }
        public Guid MenuItemId { get; set; }
        public virtual MenuItem MenuItem { get; set; } = null!;

        public required string Name { get; set; }
        public OptionGroupType OptionType { get; set; }
        public bool IsRequired { get; set; }
        public virtual ICollection<OptionItem> OptionItems { get; set; } = new List<OptionItem>();

        public bool CanDelete()
        {
            return !OptionItems.Any();
        }

        public DomainResult SoftDelete()
        {
            if (!CanDelete())
            {
                return DomainResult.Failure(DomainErrors.OptionGroup.CannotDeleteWithOptions);
            }

            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public bool CanHaveMultipleSelections()
        {
            return OptionType == OptionGroupType.Multiple;
        }

        public bool RequiresSelection()
        {
            return IsRequired;
        }

        public int GetOptionItemsCount()
        {
            return OptionItems.Count;
        }

        public decimal GetMaxExtraPrice()
        {
            if (!OptionItems.Any()) return 0;
            return OptionItems.Max(oi => oi.ExtraPrice);
        }

        public decimal GetMinExtraPrice()
        {
            if (!OptionItems.Any()) return 0;
            return OptionItems.Min(oi => oi.ExtraPrice);
        }
    }
}
