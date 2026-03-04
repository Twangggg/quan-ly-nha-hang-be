using System;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsQueue
{
    public class GetKdsQueueHandler
        : IRequestHandler<GetKdsQueueQuery, Result<List<KdsQueueResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly ILogger<GetKdsQueueHandler> _logger;

        public GetKdsQueueHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            KdsPriorityCalculator priorityCalculator,
            ILogger<GetKdsQueueHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _priorityCalculator = priorityCalculator;
            _logger = logger;
        }

        public async Task<Result<List<KdsQueueResponse>>> Handle(
            GetKdsQueueQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Fetching KDS Queue for Station: {Station}", request.Station);

            var targetStations = new List<string> { request.Station };
            if (request.Station.Equals("Kitchen", StringComparison.OrdinalIgnoreCase))
            {
                targetStations.Add(Station.HotKitchen.ToString());
                targetStations.Add(Station.ColdKitchen.ToString());
            }

            var query = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .AsNoTracking()
                .Where(oi =>
                    targetStations.Contains(oi.StationSnapshot)
                    && oi.Status == OrderItemStatus.Preparing
                )
                .Include(oi => oi.Order)
                .OrderBy(oi => oi.CreatedAt)
                .Take(50)
                .ToListAsync(cancellationToken);

            var items = query.Select(oi => new KdsQueueResponse
            {
                OrderItemId = oi.OrderItemId,
                OrderId = oi.OrderId,
                OrderCode = oi.Order != null ? oi.Order.OrderCode : string.Empty,
                ItemNameSnapshot = oi.ItemNameSnapshot,
                StationSnapshot = oi.StationSnapshot,
                Quantity = oi.Quantity,
                ItemNote = oi.ItemNote,
                CreatedAt = oi.CreatedAt,
                IsOrderPriority = oi.Order != null && oi.Order.IsPriority,
                OrderType = oi.Order != null ? oi.Order.OrderType.ToString() : string.Empty,
                TotalOrderItems = oi.Order?.OrderItems?.Count ?? 0,
                FinishedOrderItems = oi.Order?.OrderItems?.Count(x =>
                    x.Status == OrderItemStatus.Completed || x.Status == OrderItemStatus.Ready
                ) ?? 0,
                ExpectedTimeSeconds = (oi.MenuItem != null ? oi.MenuItem.ExpectedTime : 0) * 60,
            }).ToList();

            // Tính điểm ưu tiên
            foreach (var response in items)
            {
                if (Enum.TryParse<OrderType>(response.OrderType, out var orderType))
                {
                    response.PriorityScore = _priorityCalculator.Calculate(
                        response.CreatedAt,
                        response.IsOrderPriority,
                        response.ExpectedTimeSeconds,
                        orderType,
                        response.TotalOrderItems,
                        response.FinishedOrderItems
                    );
                }
            }

            // Sắp xếp theo điểm trước khi gán vị trí hàng chờ
            var sortedQueue = items
                .OrderByDescending(oi => oi.PriorityScore)
                .ThenBy(oi => oi.CreatedAt)
                .ToList();

            for (int i = 0; i < sortedQueue.Count; i++)
            {
                sortedQueue[i].QueuePosition = i + 1;
            }

            _logger.LogInformation(
                "Successfully fetched scored and paginated {Count} items in Queue for Station: {Station}",
                sortedQueue.Count,
                request.Station
            );

            return Result<List<KdsQueueResponse>>.Success(sortedQueue);
        }
    }
}
