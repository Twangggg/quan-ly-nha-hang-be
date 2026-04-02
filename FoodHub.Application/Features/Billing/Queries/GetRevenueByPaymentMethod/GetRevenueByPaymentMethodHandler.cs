using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Queries.GetRevenueByPaymentMethod
{
    public class GetRevenueByPaymentMethodHandler
        : IRequestHandler<GetRevenueByPaymentMethodQuery, Result<GetRevenueByPaymentMethodResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetRevenueByPaymentMethodHandler> _logger;

        public GetRevenueByPaymentMethodHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetRevenueByPaymentMethodHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<GetRevenueByPaymentMethodResponse>> Handle(
            GetRevenueByPaymentMethodQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting revenue by payment method. From={From}, To={To}",
                request.DateFrom, request.DateTo);

            var query = _unitOfWork.Repository<OrderPayment>()
                .Query()
                .Include(op => op.PaymentMethodConfig)
                .AsQueryable();

            // Default to today if no date range specified
            var dateFrom = request.DateFrom?.Date ?? DateTime.UtcNow.Date;
            var dateTo = request.DateTo?.Date.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1);

            query = query.Where(op => op.PaidAt >= dateFrom && op.PaidAt < dateTo);

            if (request.PaymentMethodConfigId.HasValue)
            {
                query = query.Where(op => op.PaymentMethodConfigId == request.PaymentMethodConfigId.Value);
            }

            var grouped = await query
                .GroupBy(op => new
                {
                    op.PaymentMethodConfigId,
                    op.PaymentMethodConfig.Name,
                    op.PaymentMethodConfig.Type
                })
                .Select(g => new RevenueByMethodDto
                {
                    PaymentMethodConfigId = g.Key.PaymentMethodConfigId,
                    PaymentMethodName = g.Key.Name,
                    PaymentMethodType = g.Key.Type.ToString(),
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(op => op.Amount),
                })
                .OrderByDescending(r => r.TotalAmount)
                .ToListAsync(cancellationToken);

            var totalRevenue = grouped.Sum(g => g.TotalAmount);
            var totalTransactions = grouped.Sum(g => g.TransactionCount);

            // Calculate percentages
            foreach (var item in grouped)
            {
                item.Percentage = totalRevenue > 0
                    ? Math.Round(item.TotalAmount / totalRevenue * 100, 2)
                    : 0;
            }

            var response = new GetRevenueByPaymentMethodResponse
            {
                TotalRevenue = totalRevenue,
                TotalTransactions = totalTransactions,
                Items = grouped,
            };

            return Result<GetRevenueByPaymentMethodResponse>.Success(response);
        }
    }
}
