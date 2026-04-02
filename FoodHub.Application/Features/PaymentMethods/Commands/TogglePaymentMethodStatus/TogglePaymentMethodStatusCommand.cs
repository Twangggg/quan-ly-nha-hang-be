using System.Text.Json.Serialization;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.PaymentMethods.Commands.TogglePaymentMethodStatus
{
    public class TogglePaymentMethodStatusCommand : IRequest<Result<bool>>
    {
        [JsonIgnore]
        public Guid PaymentMethodConfigId { get; set; }
    }
}
