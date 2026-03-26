using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Invoices.Queries.GetInvoicePdf
{
    public record GetInvoicePdfQuery(Guid InvoiceId) : IRequest<Result<byte[]>>;
}
