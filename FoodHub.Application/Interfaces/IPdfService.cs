using FoodHub.Application.Features.Billing.Commands.CheckoutOrder;
using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;

namespace FoodHub.Application.Interfaces
{
    public interface IPdfService
    {
        byte[] GeneratePreCheckBill(GetPreCheckBillResponse data);

        //byte[] GenerateInvoice(CheckoutOrderResponse data);
    }
}
