using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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
                .AsNoTracking()
                .Where(o => o.OrderId == request.OrderId)
                .Select(o => new PreCheckBillOrderSnapshot
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    Status = o.Status,
                    TableNumber = o.Table != null ? o.Table.TableNumber : (int?)null,
                    ReservationId = o.ReservationId,
                    EmployeeName = o.CreatedByEmployee != null ? o.CreatedByEmployee.FullName : string.Empty,
                    CustomerName = o.Reservation != null ? o.Reservation.CustomerName : null,
                    CustomerPhone = o.Reservation != null ? o.Reservation.CustomerPhone : null,
                    Items = o.OrderItems
                        .Where(oi =>
                            oi.Status != OrderItemStatus.Cancelled
                            && oi.Status != OrderItemStatus.Rejected
                        )
                        .Select(oi => new PreCheckBillItemSnapshot
                        {
                            ItemName = oi.ItemNameSnapshot,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPriceSnapshot,
                            OptionValues = oi.OptionGroups
                                .SelectMany(og => og.OptionValues)
                                .Select(ov => new PreCheckBillOptionValueSnapshot
                                {
                                    Label = ov.LabelSnapshot,
                                    Quantity = ov.Quantity,
                                    ExtraPrice = ov.ExtraPriceSnapshot,
                                })
                                .ToList(),
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(cancellationToken);

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
                    $"{_messageService.GetMessage(MessageKeys.Order.InvalidActionWithStatus)} {order.Status}",
                    ResultErrorType.BadRequest
                );
            }



            var items = order.Items
                .Select(oi =>
                {
                    var optionsSummary = BuildOptionsSummary(oi.OptionValues);
                    var lineTotal = oi.Quantity
                        * (oi.UnitPrice + oi.OptionValues.Sum(ov => ov.ExtraPrice * ov.Quantity));

                    return new PreCheckBillItemDto
                    {
                        ItemName = oi.ItemName,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        OptionsSummary = optionsSummary,
                        LineTotal = lineTotal,
                    };
                })
                .ToList();

            var subTotal = items.Sum(i => i.LineTotal);
            var discount = 0m;
            var preTaxAmount = subTotal - discount;
            var vatRate = 0m; // 10%
            var vat = preTaxAmount * (vatRate / 100m);
            var totalAmount = preTaxAmount + vat;

            var response = new GetPreCheckBillResponse
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                TableNumber = order.TableNumber,
                ReservationId = order.ReservationId,
                EmployeeName = order.EmployeeName,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                PrintedAt = DateTime.UtcNow,
                Items = items,
                SubTotal = subTotal,
                PreTaxAmount = preTaxAmount,
                Discount = discount,
                VatRate = vatRate,
                Vat = vat,
                TotalAmount = totalAmount,
            };

            _logger.LogInformation(
                "Successfully generated pre-check bill for OrderId: {OrderId}",
                request.OrderId
            );

            return Result<GetPreCheckBillResponse>.Success(response);
        }

        private static string? BuildOptionsSummary(
            IReadOnlyCollection<PreCheckBillOptionValueSnapshot> optionValues
        )
        {
            if (optionValues.Count == 0)
                return null;

            var parts = optionValues
                .Select(ov =>
                    ov.Quantity > 1 ? $"{ov.Label} x{ov.Quantity}" : ov.Label
                );

            var summary = string.Join(", ", parts);
            return string.IsNullOrEmpty(summary) ? null : summary;
        }

        private sealed class PreCheckBillOrderSnapshot
        {
            public Guid OrderId { get; init; }
            public string OrderCode { get; init; } = string.Empty;
            public OrderStatus Status { get; init; }
            public int? TableNumber { get; init; }
            public Guid? ReservationId { get; init; }
            public string EmployeeName { get; init; } = string.Empty;
            public string? CustomerName { get; init; }
            public string? CustomerPhone { get; init; }
            public List<PreCheckBillItemSnapshot> Items { get; init; } = new();
        }

        private sealed class PreCheckBillItemSnapshot
        {
            public string ItemName { get; init; } = string.Empty;
            public int Quantity { get; init; }
            public decimal UnitPrice { get; init; }
            public List<PreCheckBillOptionValueSnapshot> OptionValues { get; init; } = new();
        }

        private sealed class PreCheckBillOptionValueSnapshot
        {
            public string Label { get; init; } = string.Empty;
            public int Quantity { get; init; }
            public decimal ExtraPrice { get; init; }
        }
    }
}
