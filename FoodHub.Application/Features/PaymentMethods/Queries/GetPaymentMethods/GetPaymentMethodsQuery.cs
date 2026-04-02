using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.PaymentMethods.Queries.GetPaymentMethods
{
    public class GetPaymentMethodsQuery : IRequest<Result<List<GetPaymentMethodsResponse>>>
    {
        /// <summary>
        /// If true, returns only active payment methods (for Cashier checkout UI).
        /// If false or null, returns all (for Manager settings page).
        /// </summary>
        public bool? ActiveOnly { get; set; }
    }
}
