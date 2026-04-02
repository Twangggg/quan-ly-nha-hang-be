using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.PaymentMethods.Commands.CreatePaymentMethod
{
    public class CreatePaymentMethodCommand : IRequest<Result<CreatePaymentMethodResponse>>
    {
        public string Name { get; set; } = null!;
        public PaymentMethodType Type { get; set; }

    }
}
