using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.PaymentMethods.Commands.CreatePaymentMethod
{
    public class CreatePaymentMethodResponse : IMapFrom<PaymentMethodConfig>
    {
        public Guid PaymentMethodConfigId { get; set; }
        public string Name { get; set; } = null!;
        public PaymentMethodType Type { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }

    }
}
