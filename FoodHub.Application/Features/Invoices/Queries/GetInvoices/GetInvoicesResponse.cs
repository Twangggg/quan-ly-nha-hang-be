using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Invoices.Queries.GetInvoices
{
    public class GetInvoicesResponse
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public DateTime CreatedAt { get; set; }

        public string CashierName { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }

        public decimal TotalAmount { get; set; }
    }
}
