using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateOrderHandler> _logger;

        public CreateOrderHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<CreateOrderHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            CreateOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Creating new order. Type: {OrderType}, Table: {TableId}",
                request.OrderType,
                request.TableId
            );
            // 1. Validate Basic Logic
            if (request.OrderType == OrderType.DineIn && request.TableId == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.SelectTable),
                    ResultErrorType.BadRequest
                );
            }

            // 2. Generate Order Code: ORD-yyyyMMdd-XXXX
            // Example: ORD-20231027-0001
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"ORD-{today}-";

            // Find max code for today
            // Note: This needs to be carefully handled for concurrency in high-load systems.
            // For now, using standard approach with transaction isolation or lock logic
            // inside SaveChanges is ideal, but here we query max.
            var lastOrder = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o => o.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);

            int nextSequence = 1;
            if (lastOrder != null)
            {
                var lastCode = lastOrder.OrderCode;
                var lastSequencePart = lastCode.Split('-').LastOrDefault();
                if (int.TryParse(lastSequencePart, out int lastSeq))
                {
                    nextSequence = lastSeq + 1;
                }
            }
            var newOrderCode = $"{prefix}{nextSequence:D4}";

            // 3. Create Order
            var newOrder = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = newOrderCode,
                OrderType = request.OrderType,
                // Per discussion: Start at SERVING immediately
                Status = OrderStatus.Serving,
                TableId = request.TableId,
                Note = request.Note,
                TotalAmount = 0, // Initial amount is 0
                IsPriority = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.TryParse(_currentUserService.UserId, out var userId)
                    ? userId
                    : Guid.Empty,
            };

            // If TableId provided, we might want to check if table exists or is occupied?
            // Assuming simplified logic for now as requested in plan.

            // 4. Save
            await _unitOfWork.Repository<Order>().AddAsync(newOrder);
            await _unitOfWork.SaveChangeAsync();

            return Result<Guid>.Success(newOrder.OrderId);
        }
    }
}
