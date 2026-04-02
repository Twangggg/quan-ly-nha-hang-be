using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.PaymentMethods.Queries.GetPaymentMethodById
{
    public class GetPaymentMethodByIdQuery : IRequest<Result<GetPaymentMethodByIdResponse>>
    {
        public Guid PaymentMethodConfigId { get; set; }
    }
}
