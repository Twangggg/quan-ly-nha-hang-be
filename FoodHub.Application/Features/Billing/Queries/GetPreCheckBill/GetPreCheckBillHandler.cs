using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Queries.GetPreCheckBill
{
    public class GetPreCheckBillHandler
        : IRequestHandler<GetPreCheckBillQuery, Result<GetPreCheckBillResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetPreCheckBillHandler> _logger;

        public GetPreCheckBillHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ILogger<GetPreCheckBillHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetPreCheckBillResponse>> Handle(
            GetPreCheckBillQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Generating pre-check bill for OrderId: {OrderId}",
                request.OrderId
            );

            var order = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .Include(o => o.Table)
                .Include(o => o.CreatedByEmployee)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning(
                    "Order not found for pre-check bill. OrderId: {OrderId}",
                    request.OrderId
                );
                return Result<GetPreCheckBillResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound
                );
            }

            if (order.Status != OrderStatus.Serving)
            {
                _logger.LogWarning(
                    "Order not in Serving status for pre-check bill. OrderId: {OrderId}, Status: {Status}",
                    request.OrderId,
                    order.Status
                );
                return Result<GetPreCheckBillResponse>.Failure(
                    _messageService.GetMessage(
                        MessageKeys.Order.InvalidActionWithStatus,
                        new { Status = order.Status.ToString() }
                    ),
                    ResultErrorType.BadRequest
                );
            }

            var validItems = order
                .OrderItems.Where(oi =>
                    oi.Status != OrderItemStatus.Cancelled
                    && oi.Status != OrderItemStatus.Rejected
                )
                .ToList();

            if (!validItems.Any())
            {
                _logger.LogWarning(
                    "No valid items in order for pre-check bill. OrderId: {OrderId}",
                    request.OrderId
                );
                return Result<GetPreCheckBillResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.NoValidItems),
                    ResultErrorType.BadRequest
                );
            }

            var items = validItems
                .Select(oi =>
                {
                    var optionsSummary = BuildOptionsSummary(oi);

                    return new PreCheckBillItemDto
                    {
                        ItemName = oi.ItemNameSnapshot,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPriceSnapshot,
                        OptionsSummary = optionsSummary,
                        LineTotal = oi.GetTotalPrice(),
                    };
                })
                .ToList();

            var subTotal = items.Sum(i => i.LineTotal);

            var response = new GetPreCheckBillResponse
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                TableNumber = order.Table?.TableNumber,
                EmployeeName = order.CreatedByEmployee?.FullName ?? string.Empty,
                PrintedAt = DateTime.UtcNow,
                Items = items,
                SubTotal = subTotal,
                Discount = 0,
                Vat = 0,
                TotalAmount = subTotal,
            };

            _logger.LogInformation(
                "Successfully generated pre-check bill for OrderId: {OrderId}",
                request.OrderId
            );

            return Result<GetPreCheckBillResponse>.Success(response);
        }

        private static string? BuildOptionsSummary(OrderItem item)
        {
            if (item.OptionGroups == null || !item.OptionGroups.Any())
                return null;

            var parts = item
                .OptionGroups.SelectMany(og => og.OptionValues)
                .Select(ov =>
                    ov.Quantity > 1 ? $"{ov.LabelSnapshot} x{ov.Quantity}" : ov.LabelSnapshot
                );

            var summary = string.Join(", ", parts);
            return string.IsNullOrEmpty(summary) ? null : summary;
        }
    }
}
