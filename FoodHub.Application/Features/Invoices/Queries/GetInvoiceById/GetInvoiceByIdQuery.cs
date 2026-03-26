using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Invoices.Queries.GetInvoiceById
{
    public record GetInvoiceByIdQuery(Guid InvoiceId) : IRequest<Result<GetInvoiceByIdResponse>>;
}
