using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.Billing.Commands.CreateQrPayment
{
    public class CreateQrPaymentCommand : IRequest<Result<PaymentLinkResponse>>
    {
        public Guid OrderId { get; set; }
    }
}
