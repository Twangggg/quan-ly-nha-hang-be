using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Orders.Commands.ApplyPromotion
{
    public class ApplyPromotionCommand : IRequest<Result<ApplyPromotionResponse>>
    {
        public Guid OrderId { get; set; }
        public string Code { get; set; } = default!;
    }
}
