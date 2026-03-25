using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Promotions.Common;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Promotions.Commands.UpdatePromotion
{
    public class UpdatePromotionCommand : IRequest<Result<PromotionResponse>>
    {
        public Guid PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public PromotionType Type { get; set; }
        public decimal Value { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public Guid? ItemId { get; set; }
        public int? FreeQuantity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; }
        public int? UsageLimit { get; set; }
    }
}
