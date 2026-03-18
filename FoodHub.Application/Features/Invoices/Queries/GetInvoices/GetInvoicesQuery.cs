using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Invoices.Queries.GetInvoices
{
    public record GetInvoicesQuery : IRequest<Result<PagedResult<GetInvoicesResponse>>>
    {
        public PaginationParams Pagination { get; set; } = new PaginationParams();
        public string? Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
