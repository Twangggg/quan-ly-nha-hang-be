using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Kds;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.KDS.Common
{
    public class KdsAutoPullService : IKdsAutoPullService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<KdsAutoPullService> _logger;

        public KdsAutoPullService(
            IUnitOfWork unitOfWork,
            IKdsSettingsProvider kdsSettingsProvider,
            KdsPriorityCalculator priorityCalculator,
            ISignalRService signalRService,
            ILogger<KdsAutoPullService> logger
        )
        {
            _unitOfWork = unitOfWork;
            _kdsSettingsProvider = kdsSettingsProvider;
            _priorityCalculator = priorityCalculator;
            _signalRService = signalRService;
            _logger = logger;
        }

        public async Task<Dictionary<string, int>> GetAvailableSlotsAsync(
            IEnumerable<string> stations,
            CancellationToken cancellationToken
        )
        {
            var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);
            var result = new Dictionary<string, int>();

            foreach (var station in stations.Distinct())
            {
                var stationConfig = settings.StationWipLimits.FirstOrDefault(s =>
                    s.Station.ToString().Equals(station, StringComparison.OrdinalIgnoreCase) && s.Enabled
                );
                
                // Fallback to 4 if not found or disabled
                var limit = stationConfig?.Limit ?? 4; 

                var currentCookingCount = await _unitOfWork
                    .Repository<OrderItem>()
                    .Query()
                    .CountAsync(
                        oi => oi.StationSnapshot == station && oi.Status == OrderItemStatus.Cooking,
                        cancellationToken
                    );

                result[station] = Math.Max(0, limit - currentCookingCount);
            }

            return result;
        }

        public async Task ProcessAutoPullAsync(
            string station,
            Guid employeeId,
            CancellationToken cancellationToken
        )
        {
            var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);
            var stationConfig = settings.StationWipLimits.FirstOrDefault(s =>
                s.Station.ToString().Equals(station, StringComparison.OrdinalIgnoreCase) && s.Enabled
            );
            var limit = stationConfig?.Limit ?? 4;

            var currentCookingCount = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .CountAsync(
                    oi => oi.StationSnapshot == station && oi.Status == OrderItemStatus.Cooking,
                    cancellationToken
                );

            int availableSlots = Math.Max(0, limit - currentCookingCount);
            if (availableSlots <= 0)
                return;

            _logger.LogInformation(
                "Attempting auto-pull for Station: {Station}. Available Slots: {Slots}",
                station,
                availableSlots
            );

            var pendingItems = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .Include(oi => oi.Order)
                .Include(oi => oi.MenuItem)
                .Where(oi => oi.StationSnapshot == station && oi.Status == OrderItemStatus.Preparing)
                .ToListAsync(cancellationToken);

            if (!pendingItems.Any())
                return;

            var sortedItems = _priorityCalculator.SortQueue(
                pendingItems,
                settings.SortMode,
                oi =>
                    _priorityCalculator.Calculate(
                        settings,
                        oi.CreatedAt,
                        oi.Order?.IsPriority ?? false,
                        (oi.MenuItem?.ExpectedTime ?? 0) * 60,
                        oi.Order?.OrderType ?? OrderType.DineIn,
                        oi.Order?.OrderItems?.Count ?? 0,
                        oi.Order?.OrderItems?.Count(x => x.Status == OrderItemStatus.Completed) ?? 0
                    ),
                oi => oi.CreatedAt
            );

            var itemsToPull = sortedItems.Take(availableSlots).ToList();
            foreach (var item in itemsToPull)
            {
                _logger.LogInformation(
                    "Auto-pulling next item: {NextItemId} for Station: {Station}",
                    item.OrderItemId,
                    station
                );

                item.StartCooking();

                var autoPullLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = item.OrderId,
                    EmployeeId = employeeId,
                    Action = AuditLogActions.KdsStartCooking,
                    OldValue = $"\"{OrderItemStatus.Preparing}\"",
                    NewValue = $"\"{OrderItemStatus.Cooking}\"",
                    ChangeReason = "Auto-pull (Capacity Available)",
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(autoPullLog);

                _unitOfWork.Repository<OrderItem>().Update(item);

                // Notify SignalR Status Refresh for this item
                var response = KdsMappingHelper.MapToResponse(item, _priorityCalculator, settings);
                await _signalRService.NotifyKdsItemUpdatedAsync(station, response);
                
                await _signalRService.NotifyOrderItemStatusChangedAsync(
                    item.OrderItemId,
                    OrderItemStatus.Cooking,
                    station
                );
            }
        }
    }
}
