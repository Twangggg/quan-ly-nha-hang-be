using System;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;

namespace FoodHub.Domain.Entities
{
    public class OptionItem : BaseEntity
    {
        public Guid OptionItemId { get; set; }
        public Guid OptionGroupId { get; set; }
        public virtual OptionGroup OptionGroup { get; set; } = null!;

        public required string Label { get; set; }
        public decimal ExtraPrice { get; set; }

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
