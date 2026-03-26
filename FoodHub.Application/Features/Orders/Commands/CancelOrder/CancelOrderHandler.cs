using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ISignalRService _signalRService;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CancelOrderHandler> _logger;

        public CancelOrderHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<CancelOrderHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            CancelOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var auditorId = _currentUserService.GetUserIdAsGuid();
            if (auditorId == null)
            {
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var order = await _unitOfWork
                .Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == request.OrderId && o.Status == OrderStatus.Serving
                );

            if (order == null)
            {
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            var domainResult = order.Cancel();
            if (!domainResult.IsSuccess)
            {
                // Map Domain Error to Application Message
                return Result<bool>.Failure(
                    _messageService.GetMessage(
                        domainResult.ErrorCode ?? MessageKeys.Order.InvalidAction
                    )
                );
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            var auditLog = OrderAuditLog.CreateOrderCancelled(
                order.OrderId,
                auditorId.Value,
                request.Reason
            );

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

            bool isTableFreed = false;
            Guid freedTableId = Guid.Empty;

            // Giải phóng bàn nếu là ăn tại chỗ
            if (order.OrderType == OrderType.DineIn && order.TableId.HasValue)
            {
                var table = await _unitOfWork
                    .Repository<Domain.Entities.Table>()
                    .Query()
                    .Include(t => t.Orders)
                    .FirstOrDefaultAsync(t => t.TableId == order.TableId, cancellationToken);
                if (table != null)
                {
                    // Chuyển bàn về Available. Lúc này order đã được đổi status sang Cancelled trong bộ nhớ
                    // (hoặc nếu cần chắc chắn hơn, ta có thể dùng MarkAsAvailable trực tiếp)
                    if (table.SetAvailable())
                    {
                        table.UpdatedAt = DateTime.UtcNow;
                        table.UpdatedBy = auditorId;
                        _unitOfWork.Repository<Domain.Entities.Table>().Update(table);

                        // Ngắt kết nối đơn hàng với bàn sau khi đã giải phóng bàn xong
                        freedTableId = order.TableId.Value;
                        isTableFreed = true;

                        order.TableId = null;
                        _unitOfWork.Repository<Domain.Entities.Order>().Update(order);
                    }
                }
            }

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _cacheService.RemoveByPatternAsync(
                    CacheKey.TableList + "*",
                    cancellationToken
                );
                await _cacheService.RemoveByPatternAsync(
                    string.Format(CacheKey.TableListByArea, "*"),
                    cancellationToken
                );
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error occurred while updating order items for OrderId {OrderId}",
                    request.OrderId
                );
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError)
                );
            }

            if (isTableFreed)
            {
                await _signalRService.NotifyTableStatusChangedAsync(freedTableId, "Available");
            }

            return Result<bool>.Success(true);
        }
    }
}
