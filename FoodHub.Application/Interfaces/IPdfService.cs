using FoodHub.Application.Features.Billing.Commands.CheckoutOrder;
using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces
{
    public interface IPdfService
    {
        byte[] GeneratePreCheckBill(GetPreCheckBillResponse data);

        byte[] GenerateInvoicePdf(Invoice invoice);
    }
}
