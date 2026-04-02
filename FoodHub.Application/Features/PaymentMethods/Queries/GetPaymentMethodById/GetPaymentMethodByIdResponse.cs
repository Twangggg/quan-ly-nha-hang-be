using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.PaymentMethods.Queries.GetPaymentMethodById
{
    public class GetPaymentMethodByIdResponse : IMapFrom<PaymentMethodConfig>
    {
        public Guid PaymentMethodConfigId { get; set; }
        public string Name { get; set; } = null!;
        public PaymentMethodType Type { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public string? BankName { get; set; }
        public string? BankBin { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolderName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
