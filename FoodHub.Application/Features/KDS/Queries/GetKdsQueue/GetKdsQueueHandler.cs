using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Kds;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly ILogger<GetKdsQueueHandler> _logger;

        public GetKdsQueueHandler(
            IUnitOfWork unitOfWork,
            KdsPriorityCalculator priorityCalculator,
            IKdsSettingsProvider kdsSettingsProvider,
            ILogger<GetKdsQueueHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _priorityCalculator = priorityCalculator;
            _kdsSettingsProvider = kdsSettingsProvider;
            _logger = logger;
        }

        public async Task<Result<List<KdsQueueResponse>>> Handle(
            GetKdsQueueQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Fetching KDS Queue for Station: {Station}", request.Station);

            var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);
            var targetStations = KdsStationHelper.ExpandRequestedStations(request.Station);
            var stationKey = request.Station?.ToLowerInvariant() ?? "hotkitchen";
            var wipLimit = KdsStationHelper.GetWipLimitForStation(settings, stationKey);

            var activeItemsCount = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .AsNoTrackingWithIdentityResolution()
                .Where(oi =>
                    targetStations.Contains(oi.StationSnapshot)
                    && (
                        oi.Status == OrderItemStatus.Preparing
                        || oi.Status == OrderItemStatus.Cooking
                    )
                )
                .CountAsync(cancellationToken);

            var query = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .AsNoTrackingWithIdentityResolution()
                .Where(oi =>
                    targetStations.Contains(oi.StationSnapshot)
                    && oi.Status == OrderItemStatus.Preparing
                )
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.OrderItems)
                .Include(oi => oi.MenuItem)
                .Include(oi => oi.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                .OrderBy(oi => oi.CreatedAt)
                .Take(50)
                .ToListAsync(cancellationToken);

            var items = query
                .Select(oi =>
                {
                    var orderType = oi.Order?.OrderType ?? OrderType.DineIn;
                    var isOrderPriority = oi.Order?.IsPriority ?? false;
                    var totalOrderItems = oi.Order?.GetCountableKitchenItems().Count ?? 0;
                    var finishedOrderItems =
                        oi.Order?.GetCountableKitchenItems().Count(x => x.Status == OrderItemStatus.Completed) ?? 0;
                    var expectedTimeSeconds = (oi.MenuItem?.ExpectedTime ?? 0) * 60;

                    return new KdsQueueResponse
                    {
                        OrderItemId = oi.OrderItemId,
                        OrderId = oi.OrderId,
                        OrderCode = oi.Order?.OrderCode ?? string.Empty,
                        ItemNameSnapshot = oi.ItemNameSnapshot,
                        StationSnapshot = oi.StationSnapshot,
                        Quantity = oi.Quantity,
                        ItemNote = oi.ItemNote,
                        Status = oi.Status.ToString(),
                        CreatedAt = oi.CreatedAt,
                        IsOrderPriority = isOrderPriority,
                        IsPriority = isOrderPriority,
                        OrderType = orderType.ToString(),
                        TotalOrderItems = totalOrderItems,
                        FinishedOrderItems = finishedOrderItems,
                        ExpectedTimeSeconds = expectedTimeSeconds,
                        PriorityScore = _priorityCalculator.Calculate(
                            settings,
                            oi.CreatedAt,
                            isOrderPriority,
                            expectedTimeSeconds,
                            orderType,
                            totalOrderItems,
                            finishedOrderItems
                        ),
                        ItemOptions = string.Join(
                            ", ",
                            (oi.OptionGroups ?? Enumerable.Empty<OrderItemOptionGroup>())
                                .SelectMany(g =>
                                    g.OptionValues ?? Enumerable.Empty<OrderItemOptionValue>()
                                )
                                .Select(v =>
                                    v.Quantity > 1
                                        ? $"{v.LabelSnapshot} x{v.Quantity}"
                                        : v.LabelSnapshot
                                )
                        ),
                    };
                })
                .ToList();

            var sortedQueue = _priorityCalculator.SortQueue(
                items,
                settings.SortMode,
                item => item.PriorityScore,
                item => item.CreatedAt
            );

            for (int i = 0; i < sortedQueue.Count; i++)
            {
                sortedQueue[i].QueuePosition = i + 1;
            }

            _logger.LogInformation(
                "Successfully fetched scored and paginated {Count} items in Queue for Station: {Station} (WIP limit: {WipLimit}, Active: {ActiveCount})",
                sortedQueue.Count,
                request.Station,
                wipLimit,
                activeItemsCount
            );

            return Result<List<KdsQueueResponse>>.Success(sortedQueue);
        }
    }
}
