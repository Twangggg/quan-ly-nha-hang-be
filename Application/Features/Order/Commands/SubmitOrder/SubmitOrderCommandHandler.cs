using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Order.Commands.SubmitOrder
{
    public class SubmitOrderCommandHandler : IRequestHandler<SubmitOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public SubmitOrderCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
        {
            //Get current user id
            var currentIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentIdString) || !Guid.TryParse(currentIdString, out var userId))
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn));
            }

            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);

            if(order == null)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound));
            }

            // if(order.Status != Domain.Enums.OrderStatus.Draft)
            // {
            //     return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus) + $"{order.Status}");
            // }

            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.MustHaveItem));
            }

            if (order.OrderType == Domain.Enums.OrderType.DineIn)
            {
                if(order.TableId == null)
                {
                    return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.SelectTable));
                }
            }

            // Update status
            order.Status = Domain.Enums.OrderStatus.Serving;
            order.SubmittedAt = DateTime.UtcNow;

            // Set all items status to Preparing
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    item.Status = Domain.Enums.OrderItemStatus.Preparing;
                }
            }

            // Audit Log
            var auditLog = new Domain.Entities.OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = userId,
                Action = "SUBMIT",
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<Domain.Entities.OrderAuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result<Guid>.Success(order.OrderId);
        }


    }
}
