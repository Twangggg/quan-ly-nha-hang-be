using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderCommand : IRequest<Result<Guid>>
    {
        [JsonIgnore]
        public Guid OrderId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal? AmountReceived { get; set; }
    }
}
