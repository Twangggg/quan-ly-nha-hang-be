using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;

namespace FoodHub.Application.Interfaces.Reporting
{
    public interface IPdfService
    {
        byte[] GeneratePreCheckBill(GetPreCheckBillResponse data);
    }
}
