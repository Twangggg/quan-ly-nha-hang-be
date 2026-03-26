using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsBacklogSummary
{
    public class GetKdsBacklogSummaryHandler
        : IRequestHandler<GetKdsBacklogSummaryQuery, Result<GetKdsBacklogSummaryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private const int DelayedThresholdMinutes = 20;

        public GetKdsBacklogSummaryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<GetKdsBacklogSummaryResponse>> Handle(
            GetKdsBacklogSummaryQuery request,
            CancellationToken cancellationToken
        )
        {
            var now = DateTime.UtcNow;
            var delayedThreshold = now.AddMinutes(-DelayedThresholdMinutes);

            var allItems = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .AsNoTracking()
                .Where(oi => 
                    oi.Status == OrderItemStatus.Preparing || 
                    oi.Status == OrderItemStatus.Cooking
                )
                .ToListAsync(cancellationToken);

            var totalProcessingItems = allItems.Count;
            var waitingCount = allItems.Count(oi => oi.Status == OrderItemStatus.Preparing);
            var preparingCount = allItems.Count(oi => oi.Status == OrderItemStatus.Cooking);
            var delayedCount = allItems.Count(oi => 
                oi.CreatedAt <= delayedThreshold && 
                (oi.Status == OrderItemStatus.Preparing || oi.Status == OrderItemStatus.Cooking)
            );

            var preparingPercentage = totalProcessingItems > 0
                ? Math.Round((double)preparingCount / totalProcessingItems * 100, 2)
                : 0;

            return Result<GetKdsBacklogSummaryResponse>.Success(
                new GetKdsBacklogSummaryResponse
                {
                    TotalProcessingItems = totalProcessingItems,
                    WaitingCount = waitingCount,
                    PreparingCount = preparingCount,
                    DelayedCount = delayedCount,
                    PreparingPercentage = preparingPercentage,
                }
            );
        }
    }
}
