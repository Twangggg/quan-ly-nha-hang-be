using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.CompleteOrder
{
    public class CompleteOrderHandler
        : IRequestHandler<CompleteOrderCommand, Result<CompleteOrderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CompleteOrderCommand> _logger;

        public CompleteOrderHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ICurrentUserService currentUserService,
            ILogger<CompleteOrderCommand> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<CompleteOrderResponse>> Handle(
            CompleteOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<CompleteOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            var order = await _unitOfWork
                .Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == request.OrderId && o.Status != Domain.Enums.OrderStatus.Completed
                );

            if (order == null)
            {
                return Result<CompleteOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            var domainResult = order.Complete();
            if (!domainResult.IsSuccess)
            {
                return Result<CompleteOrderResponse>.Failure(
                    _messageService.GetMessage(
                        domainResult.ErrorCode ?? MessageKeys.Order.InvalidAction
                    )
                );
            }

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = auditorId,
                Action = AuditLogActions.CompleteOrder,
                CreatedAt = DateTime.UtcNow,
                NewValue =
                    $"{{\"finalAmount\": {order.TotalAmount}, \"itemsCount\": {order.OrderItems.Count}}}",
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error occurred while completing OrderId {OrderId}",
                    request.OrderId
                );
                return Result<CompleteOrderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError)
                );
            }

            var response = _mapper.Map<CompleteOrderResponse>(order);
            return Result<CompleteOrderResponse>.Success(response);
        }
    }
}
