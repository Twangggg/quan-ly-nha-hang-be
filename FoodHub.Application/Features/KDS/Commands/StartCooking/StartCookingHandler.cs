using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Features.KDS.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.KDS.Commands.StartCooking
{
    public class StartCookingHandler : IRequestHandler<StartCookingCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<StartCookingHandler> _logger;

        public StartCookingHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            ILogger<StartCookingHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            StartCookingCommand request,
            CancellationToken cancellationToken
        )
        {
            var auditorId = _currentUserService.GetUserIdAsGuid();
            if (auditorId == null)
            {
                _logger.LogWarning(
                    "Unauthorized start cooking attempt for OrderItemId {OrderItemId}",
                    request.OrderItemId
                );
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }
            _logger.LogInformation(
                "Attempting to start cooking for OrderItemId: {OrderItemId}",
                request.OrderItemId
            );

            var orderItemRepository = _unitOfWork.Repository<OrderItem>();
            var orderItem = await orderItemRepository
                .Query()
                .FirstOrDefaultAsync(
                    oi => oi.OrderItemId == request.OrderItemId,
                    cancellationToken
                );

            if (orderItem == null)
            {
                _logger.LogWarning("OrderItem not found: {OrderItemId}", request.OrderItemId);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.OrderItem.NotFound),
                    ResultErrorType.NotFound
                );
            }

            // Xác định nhóm trạm
            var targetStations = new List<string> { orderItem.StationSnapshot };
            if (
                orderItem.StationSnapshot == Station.HotKitchen.ToString()
                || orderItem.StationSnapshot == Station.ColdKitchen.ToString()
            )
            {
                targetStations = new List<string>
                {
                    Station.HotKitchen.ToString(),
                    Station.ColdKitchen.ToString(),
                };
            }

            // WIP Limit Check (Max 4 items per Station Group)
            var cookingCount = await orderItemRepository
                .Query()
                .AsNoTracking()
                .CountAsync(
                    oi =>
                        targetStations.Contains(oi.StationSnapshot)
                        && oi.Status == OrderItemStatus.Cooking,
                    cancellationToken
                );

            if (cookingCount >= 4)
            {
                _logger.LogWarning(
                    "WIP limit exceeded for Station: {Station}. Current cooking: {Count}",
                    orderItem.StationSnapshot,
                    cookingCount
                );
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.KDS.WipLimitExceeded)
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var domainResult = orderItem.StartCooking();
                if (!domainResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Domain logic failed for StartCooking: {OrderItemId}. Error: {Error}",
                        request.OrderItemId,
                        domainResult.ErrorCode
                    );
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(
                            MessageKeys.OrderItem.MustBePreparingToStartCooking
                        )
                    );
                }

                var auditLog = OrderAuditLog.CreateKdsStartCooking(
                    orderItem.OrderId,
                    auditorId.Value
                );

                orderItemRepository.Update(orderItem);
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully started cooking for OrderItemId: {OrderItemId} at Station: {Station}",
                    request.OrderItemId,
                    orderItem.StationSnapshot
                );

                // SignalR Notify
                _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                    orderItem.OrderItemId,
                    OrderItemStatus.Cooking,
                    orderItem.StationSnapshot
                );

                return Result<Guid>.Success(orderItem.OrderItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while starting cooking for OrderItemId: {OrderItemId}",
                    request.OrderItemId
                );
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
