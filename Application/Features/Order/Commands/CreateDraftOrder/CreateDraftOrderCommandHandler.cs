using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Linq;

namespace FoodHub.Application.Features.Order.Commands.CreateDraftOrder
{
    public class CreateDraftOrderCommandHandler : IRequestHandler<CreateDraftOrderCommand, Result<Guid>> 
    {
        private IUnitOfWork _unitOfWork;
        private ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        public CreateDraftOrderCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(CreateDraftOrderCommand request, CancellationToken cancellationToken)
        {
            //Get current user id
            var currentIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentIdString) || !Guid.TryParse(currentIdString, out var userId))
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn));
            }

            //Gen order code (Format ORD-yyyymmdd-xxxx .... x is number order in db)
            var today = DateTime.UtcNow.Date;
            var dateString = today.ToString("yyyyMMdd");
            var prefix = $"ORD-{dateString}-";

            //Get last order code today
            var lastOrder = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Where(t => t.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);

            int sequenceNumber = 1;

            if (lastOrder != null)
            {
                var lastCode = lastOrder.OrderCode;
                var part = lastCode.Split("-");
                if (part.Length == 3 && int.TryParse(part[2], out int lastPart))
                {
                    sequenceNumber += 1;
                }
            }

            var newOrderCode = $"{prefix}{sequenceNumber:D4}";

            // ***** ***** (Hard code) QUô quô chỗ này nè
            // Bổ sung ở user table layout
            var order = new Domain.Entities.Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = newOrderCode,
                OrderType = request.OrderType,
                Status = OrderStatus.Draft,
                TableId = request.OrderType == OrderType.DineIn ? request.TableId : null,
                Note = request.Note,
                TotalAmount = 0,
                IsPriority = false,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                OrderItems = new List<OrderItem>(),
                OrderAuditLogs = new List<OrderAuditLog>()
            };

            await _unitOfWork.Repository<Domain.Entities.Order>().AddAsync(order);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<Guid>.Success(order.OrderId);

        }
    }
}
