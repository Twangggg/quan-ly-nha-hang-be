using System;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;

namespace FoodHub.Domain.Entities
{
    public class OptionItem : BaseEntity
    {
        private OptionItem() { }

        public Guid OptionItemId { get; private set; }
        public Guid OptionGroupId { get; private set; }
        public virtual OptionGroup OptionGroup { get; private set; } = null!;

        public string Label { get; private set; } = string.Empty;
        public decimal ExtraPrice { get; private set; }

        public static OptionItem Create(
            Guid optionGroupId,
            string label,
            decimal extraPrice,
            Guid? actorId = null
        )
        {
            return new OptionItem
            {
                OptionItemId = Guid.NewGuid(),
                OptionGroupId = optionGroupId,
                Label = label.Trim(),
                ExtraPrice = extraPrice,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorId,
                UpdatedBy = actorId,
            };
        }

        public void Update(string label, decimal extraPrice, Guid? actorId = null)
        {
            Label = label.Trim();
            ExtraPrice = extraPrice;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = actorId;
        }

        public DomainResult Validate()
        {
            if (ExtraPrice < 0)
            {
                return DomainResult.Failure(DomainErrors.OptionItem.InvalidExtraPrice);
            }
            return DomainResult.Success();
        }

        public bool HasExtraPrice()
        {
            return ExtraPrice > 0;
        }

        public decimal GetTotalPrice(decimal basePrice)
        {
            return basePrice + ExtraPrice;
        }
    }
}
