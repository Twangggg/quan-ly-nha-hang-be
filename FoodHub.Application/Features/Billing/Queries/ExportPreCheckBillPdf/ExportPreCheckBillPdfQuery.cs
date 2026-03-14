using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Billing.Queries.ExportPreCheckBillPdf
{
    public class ExportPreCheckBillPdfQuery : IRequest<Result<ExportPreCheckBillPdfResponse>>
    {
        public Guid OrderId { get; set; }
    }
}
