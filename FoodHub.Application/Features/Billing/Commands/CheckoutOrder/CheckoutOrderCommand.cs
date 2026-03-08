using System.Text.Json.Serialization;
using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderCommand : IRequest<Result<Guid>>
    {
        [JsonIgnore]
        public Guid OrderId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        [JsonPropertyName("amountReceived")]
        public decimal? AmountPaid { get; set; }
    }
}
