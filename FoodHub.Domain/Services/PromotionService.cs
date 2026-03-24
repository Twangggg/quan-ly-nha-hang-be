using FoodHub.Domain.Common;
using FoodHub.Domain.Entities;

namespace FoodHub.Domain.Services
{
    public class PromotionService
    {
        public DomainResult<decimal> CalculateDiscount(Order order, Promotion promotion)
        {
            var validation = promotion.Validate(order.SubTotal, DateTimeOffset.UtcNow);
            if (!validation.IsSuccess)
            {
                return DomainResult<decimal>.Failure(validation.ErrorCode!);
            }

            decimal discount = 0;
            if (promotion.Type == Enums.PromotionType.Percent)
            {
                discount = order.SubTotal * promotion.Value / 100;
                if (promotion.MaxDiscount.HasValue)
                {
                    discount = Math.Min(discount, promotion.MaxDiscount.Value);
                }
            }
            else if (promotion.Type == Enums.PromotionType.Fixed)
            {
                discount = promotion.Value;
            }

            // Ensure discount doesn't exceed subtotal
            discount = Math.Min(discount, order.SubTotal);

            return DomainResult<decimal>.Success(discount);
        }
    }
}
