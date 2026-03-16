using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Invoices.Commands.CreateInvoice
{
    public record CreateInvoiceCommand(
        Guid OrderId,
        decimal AmountReceived
        ) : IRequest<Result<CreateInvoiceResponse>>;
}
