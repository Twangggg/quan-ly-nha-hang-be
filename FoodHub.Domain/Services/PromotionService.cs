using FoodHub.Domain.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Services
{
    public class PromotionService
    {
        public DomainResult<decimal> CalculateDiscount(Order order, Promotion promotion)
        {
            var validation = promotion.Validate(
                order.GetPromotionValidationSubTotal(),
                DateTimeOffset.UtcNow
            );
            if (!validation.IsSuccess)
            {
                return DomainResult<decimal>.Failure(validation.ErrorCode!);
            }

            decimal discount = 0;
            if (promotion.Type == PromotionType.Percent)
            {
                discount = order.SubTotal * promotion.Value / 100;
                if (promotion.MaxDiscount.HasValue)
                {
                    discount = Math.Min(discount, promotion.MaxDiscount.Value);
                }
            }
            else if (promotion.Type == PromotionType.Fixed)
            {
                discount = promotion.Value;
            }

            // Ensure discount doesn't exceed subtotal
            discount = Math.Min(discount, order.SubTotal);

            return DomainResult<decimal>.Success(discount);
        }
    }
}
