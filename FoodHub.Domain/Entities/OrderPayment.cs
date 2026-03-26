namespace FoodHub.Domain.Entities
{
    public class OrderPayment : BaseEntity
    {
        public Guid OrderPaymentId { get; set; }
        public Guid OrderId { get; set; }
        public Guid PaymentMethodConfigId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }

        // Navigation
        public virtual Order Order { get; set; } = null!;
        public virtual PaymentMethodConfig PaymentMethodConfig { get; set; } = null!;
    }
}
