using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Invoices.Queries.GetInvoicePdf;
using FoodHub.Application.Interfaces;
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

            _logger.LogInformation("Initial invoice count: {Count}", await query.CountAsync(cancellationToken));
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

            _logger.LogInformation("Filtered invoice count: {Count}", await query.CountAsync(cancellationToken));
            var mappedQuery = query.Select(i => new GetInvoicesResponse
            {
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                OrderId = i.OrderId,
                CreatedAt = i.CreatedAt,
                CashierName = i.CashierName,
                TableNumber = i.TableNumber,
                PaymentMethod = i.PaymentMethod,
                TotalAmount = i.TotalAmount
            });

            _logger.LogInformation("Mapped invoice count: {Count}", await mappedQuery.CountAsync(cancellationToken));
            var response = await mappedQuery.ToPagedResultAsync(request.Pagination, cancellationToken);

            return Result<PagedResult<GetInvoicesResponse>>.Success(response);
        }
    }
}
