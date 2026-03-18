namespace FoodHub.Application.Features.Invoices.Commands.CreateInvoice
{
    public class CreateInvoiceResponse
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
    }
}
