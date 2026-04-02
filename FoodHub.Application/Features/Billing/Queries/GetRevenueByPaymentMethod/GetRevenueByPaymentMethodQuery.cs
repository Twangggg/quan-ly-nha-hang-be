using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Billing.Queries.GetRevenueByPaymentMethod
{
    public class GetRevenueByPaymentMethodQuery : IRequest<Result<GetRevenueByPaymentMethodResponse>>
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public Guid? PaymentMethodConfigId { get; set; }
    }
}
