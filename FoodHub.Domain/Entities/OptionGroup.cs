using System;
using System.Collections.Generic;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class OptionGroup : BaseEntity
    {
        private OptionGroup() { }

        public Guid OptionGroupId { get; private set; }
        public Guid? MenuItemId { get; private set; }
        public virtual MenuItem? MenuItem { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public OptionGroupType OptionType { get; private set; }
        public bool IsRequired { get; private set; }
        public virtual ICollection<OptionItem> OptionItems { get; set; } = new List<OptionItem>();
        public virtual ICollection<MenuItemOptionGroup> MenuItemOptionGroups { get; set; } =
            new List<MenuItemOptionGroup>();

        public static OptionGroup Create(
            string name,
            OptionGroupType optionType,
            bool isRequired,
            Guid? legacyMenuItemId = null,
            Guid? actorId = null
        )
        {
            return new OptionGroup
            {
                OptionGroupId = Guid.NewGuid(),
                MenuItemId = legacyMenuItemId,
                Name = name.Trim(),
                OptionType = optionType,
                IsRequired = isRequired,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorId,
                UpdatedBy = actorId,
            };
        }

        public void Update(
            string name,
            OptionGroupType optionType,
            bool isRequired,
            Guid? actorId = null
        )
        {
            Name = name.Trim();
            OptionType = optionType;
            IsRequired = isRequired;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = actorId;
        }

        public void AttachLegacyMenuItem(Guid? menuItemId, Guid? actorId = null)
        {
            MenuItemId = menuItemId;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = actorId;
        }

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
            return OptionType == OptionGroupType.Multi;
        }

        public bool RequiresSelection()
        {
            return IsRequired;
        }

        public int GetDefaultMinSelect()
        {
            return IsRequired ? 1 : 0;
        }

        public int GetDefaultMaxSelect()
        {
            return OptionType == OptionGroupType.Single ? 1 : OptionItems.Count;
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
