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

namespace FoodHub.Application.Features.KDS.Queries.GetKdsItems
{
    public class GetKdsItemsHandler
        : IRequestHandler<GetKdsItemsQuery, Result<List<KdsItemResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly ILogger<GetKdsItemsHandler> _logger;

        public GetKdsItemsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            KdsPriorityCalculator priorityCalculator,
            ILogger<GetKdsItemsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _priorityCalculator = priorityCalculator;
            _logger = logger;
        }

        public async Task<Result<List<KdsItemResponse>>> Handle(
            GetKdsItemsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Fetching KDS items for Station: {Station}", request.Station);

            var orderItemRepository = _unitOfWork.Repository<OrderItem>();

            var targetStations = new List<string> { request.Station };
            if (request.Station.Equals("Kitchen", StringComparison.OrdinalIgnoreCase))
            {
                targetStations.Add(Station.HotKitchen.ToString());
                targetStations.Add(Station.ColdKitchen.ToString());
            }

            var items = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .AsNoTracking()
                .Where(oi =>
                    targetStations.Contains(oi.StationSnapshot)
                    && (
                        oi.Status == OrderItemStatus.Preparing
                        || oi.Status == OrderItemStatus.Cooking
                    )
                )
                .Include(oi => oi.Order)
                .ToListAsync(cancellationToken);

            var responseItems = items
                .Select(oi => new KdsItemResponse
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    OrderCode = oi.Order != null ? oi.Order.OrderCode : string.Empty,
                    ItemNameSnapshot = oi.ItemNameSnapshot,
                    StationSnapshot = oi.StationSnapshot,
                    Quantity = oi.Quantity,
                    ItemNote = oi.ItemNote,
                    Status = oi.Status.ToString(),
                    RejectionReason = oi.RejectionReason,
                    CreatedAt = oi.CreatedAt,
                    IsOrderPriority = oi.Order != null && oi.Order.IsPriority,
                    OrderType = oi.Order != null ? oi.Order.OrderType.ToString() : string.Empty,
                    TotalOrderItems = oi.Order?.OrderItems?.Count ?? 0,
                    FinishedOrderItems =
                        oi.Order?.OrderItems?.Count(x =>
                            x.Status == OrderItemStatus.Completed
                            || x.Status == OrderItemStatus.Ready
                        ) ?? 0,
                    ExpectedTimeSeconds = (oi.MenuItem != null ? oi.MenuItem.ExpectedTime : 0) * 60,
                    ItemOptions = string.Join(
                        ", ",
                        (oi.OptionGroups ?? Enumerable.Empty<OrderItemOptionGroup>())
                            .SelectMany(g =>
                                g.OptionValues ?? Enumerable.Empty<OrderItemOptionValue>()
                            )
                            .Select(v =>
                                v.Quantity > 1
                                    ? v.LabelSnapshot + " x" + v.Quantity
                                    : v.LabelSnapshot
                            )
                    ),
                })
                .ToList();

            // Calculate Priority Score
            foreach (var item in responseItems)
            {
                if (Enum.TryParse<OrderType>(item.OrderType, out var orderType))
                {
                    item.PriorityScore = _priorityCalculator.Calculate(
                        item.CreatedAt,
                        item.IsOrderPriority,
                        item.ExpectedTimeSeconds,
                        orderType,
                        item.TotalOrderItems,
                        item.FinishedOrderItems
                    );
                }
            }

            // Sắp xếp lại danh sách theo thứ tự tối ưu
            var sortedItems = responseItems
                .OrderBy(oi => oi.Status == OrderItemStatus.Cooking.ToString() ? 0 : 1) // Đang nấu ưu tiên hiện trước
                .ThenByDescending(oi => oi.PriorityScore) // Điểm ưu tiên cao hơn lên trước
                .ThenBy(oi => oi.CreatedAt) // FIFO fallback: ai đến trước làm trước nếu bằng điểm
                .ToList();

            _logger.LogInformation(
                "Successfully fetched and prioritized {Count} KDS items for Station: {Station}",
                sortedItems.Count,
                request.Station
            );

            return Result<List<KdsItemResponse>>.Success(sortedItems);
        }
    }
}
