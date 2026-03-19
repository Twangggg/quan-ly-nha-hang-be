using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class MenuItemOptionGroup : BaseEntity
    {
        private MenuItemOptionGroup() { }

        public Guid MenuItemOptionGroupId { get; private set; }
        public Guid MenuItemId { get; private set; }
        public virtual MenuItem MenuItem { get; private set; } = null!;

        public Guid OptionGroupId { get; private set; }
        public virtual OptionGroup OptionGroup { get; private set; } = null!;

        public bool IsRequired { get; private set; }
        public int MinSelect { get; private set; }
        public int MaxSelect { get; private set; }
        public int SortOrder { get; private set; }
        public bool IsVisible { get; private set; } = true;

        public static MenuItemOptionGroup Create(
            Guid menuItemId,
            Guid optionGroupId,
            OptionGroupType optionGroupType,
            bool isRequired,
            int? minSelect,
            int? maxSelect,
            int sortOrder,
            bool isVisible,
            Guid? actorId = null
        )
        {
            var assignment = new MenuItemOptionGroup
            {
                MenuItemOptionGroupId = Guid.NewGuid(),
                MenuItemId = menuItemId,
                OptionGroupId = optionGroupId,
                SortOrder = sortOrder,
                IsVisible = isVisible,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorId,
                UpdatedBy = actorId,
            };

            assignment.ApplyConfiguration(optionGroupType, isRequired, minSelect, maxSelect, actorId);
            return assignment;
        }

        public void UpdateConfiguration(
            OptionGroupType optionGroupType,
            bool isRequired,
            int? minSelect,
            int? maxSelect,
            int sortOrder,
            bool isVisible,
            Guid? actorId = null
        )
        {
            SortOrder = sortOrder;
            IsVisible = isVisible;
            ApplyConfiguration(optionGroupType, isRequired, minSelect, maxSelect, actorId);
        }

        public void AttachOptionGroup(OptionGroup optionGroup)
        {
            OptionGroup = optionGroup;
        }

        private void ApplyConfiguration(
            OptionGroupType optionGroupType,
            bool isRequired,
            int? minSelect,
            int? maxSelect,
            Guid? actorId
        )
        {
            IsRequired = isRequired;
            MinSelect = minSelect ?? (isRequired ? 1 : 0);
            MaxSelect = maxSelect ?? (optionGroupType == OptionGroupType.Single ? 1 : int.MaxValue);
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = actorId;
        }
    }
}
