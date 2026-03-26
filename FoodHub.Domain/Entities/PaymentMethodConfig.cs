using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class PaymentMethodConfig : BaseEntity
    {
        public Guid PaymentMethodConfigId { get; set; }
        public string Name { get; set; } = null!;
        public PaymentMethodType Type { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }

        // Bank info – required only when Type == BankTransfer
        public string? BankName { get; set; }
        public string? BankBin { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolderName { get; set; }

        // PayOS Config - dynamic allocation per bank method
        public string? PayOsClientId { get; set; }
        public string? PayOsApiKey { get; set; }
        public string? PayOsChecksumKey { get; set; }

        // Navigation
        public ICollection<OrderPayment> OrderPayments { get; set; } = new List<OrderPayment>();

        // --- Domain Methods ---

        public DomainResult Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult Deactivate()
        {
            if (IsDefault)
            {
                return DomainResult.Failure(DomainErrors.PaymentMethodConfig.CannotDeactivateDefault);
            }

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult UpdateInfo(
            string name,
            PaymentMethodType type)
        {
            Name = name;
            Type = type;
            UpdatedAt = DateTime.UtcNow;

            return DomainResult.Success();
        }
    }
}
