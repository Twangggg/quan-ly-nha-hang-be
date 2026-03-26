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

namespace FoodHub.Application.Features.KDS.Queries.GetKdsItems
{
    public class GetKdsItemsHandler
        : IRequestHandler<GetKdsItemsQuery, Result<List<KdsItemResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly ILogger<GetKdsItemsHandler> _logger;

        public GetKdsItemsHandler(
            IUnitOfWork unitOfWork,
            KdsPriorityCalculator priorityCalculator,
            IKdsSettingsProvider kdsSettingsProvider,
            ILogger<GetKdsItemsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _priorityCalculator = priorityCalculator;
            _kdsSettingsProvider = kdsSettingsProvider;
            _logger = logger;
        }

        public async Task<Result<List<KdsItemResponse>>> Handle(
            GetKdsItemsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Fetching KDS items for Station: {Station}", request.Station);

            var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);
            var targetStations = KdsStationHelper.ExpandRequestedStations(request.Station);

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
                    .ThenInclude(o => o.OrderItems)
                .Include(oi => oi.MenuItem)
                .Include(oi => oi.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                .ToListAsync(cancellationToken);

            var responseItems = items
                .Select(oi =>
                {
                    var orderType = oi.Order?.OrderType ?? OrderType.DineIn;
                    var isOrderPriority = oi.Order?.IsPriority ?? false;
                    var totalOrderItems = oi.Order?.OrderItems?.Count ?? 0;
                    var finishedOrderItems =
                        oi.Order?.OrderItems?.Count(x => x.Status == OrderItemStatus.Completed) ?? 0;
                    var expectedTimeSeconds = (oi.MenuItem?.ExpectedTime ?? 0) * 60;

                    return new KdsItemResponse
                    {
                        OrderItemId = oi.OrderItemId,
                        OrderId = oi.OrderId,
                        OrderCode = oi.Order?.OrderCode ?? string.Empty,
                        ItemNameSnapshot = oi.ItemNameSnapshot,
                        StationSnapshot = oi.StationSnapshot,
                        Quantity = oi.Quantity,
                        ItemNote = oi.ItemNote,
                        Status = oi.Status.ToString(),
                        RejectionReason = oi.RejectionReason,
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

            var sortedItems = _priorityCalculator.SortActiveItems(
                responseItems,
                settings.SortMode,
                item => item.Status == OrderItemStatus.Cooking.ToString(),
                item => item.PriorityScore,
                item => item.CreatedAt
            );

            _logger.LogInformation(
                "Successfully fetched and prioritized {Count} KDS items for Station: {Station}",
                sortedItems.Count,
                request.Station
            );

            return Result<List<KdsItemResponse>>.Success(sortedItems);
        }
    }
}
