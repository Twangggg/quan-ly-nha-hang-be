using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Invoices.Queries.GetInvoicePdf;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Invoices.Queries.GetInvoices
{
    public class GetInvoicesHandler : IRequestHandler<GetInvoicesQuery, Result<PagedResult<GetInvoicesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetInvoicesHandler> _logger;

        public GetInvoicesHandler(IUnitOfWork unitOfWork, ILogger<GetInvoicesHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetInvoicesResponse>>> Handle(
            GetInvoicesQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Handling GetInvoicesQuery with Keyword: {Keyword}, FromDate: {FromDate}, ToDate: {ToDate}",
                request.Keyword, request.FromDate, request.ToDate);
            var invoiceRepo = _unitOfWork.Repository<Invoice>();
            var query = invoiceRepo.Query().AsNoTracking();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                var keywordLower = request.Keyword.ToLower();
                query = query.Where(i => i.InvoiceNumber.ToLower().Contains(keywordLower));
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(i => i.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(i => i.CreatedAt <= request.ToDate.Value);
            }

            query = query.OrderByDescending(i => i.CreatedAt);

            var mappedQuery = query.Select(i => new GetInvoicesResponse
            {
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                OrderId = i.OrderId,
                CreatedAt = i.CreatedAt,
                CashierName = i.CashierName,
                TableNumber = i.TableNumber ?? string.Empty,
                PaymentMethod = i.PaymentMethod,
                TotalAmount = i.TotalAmount
            });

            var response = await mappedQuery.ToPagedResultAsync(request.Pagination, cancellationToken);

            return Result<PagedResult<GetInvoicesResponse>>.Success(response);
        }
    }
}
