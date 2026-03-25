namespace FoodHub.Application.Features.Promotions.Commands.DeletePromotion
{
    public sealed record DeletePromotionResponse(Guid PromotionId, DateTime? DeletedAt);
}
