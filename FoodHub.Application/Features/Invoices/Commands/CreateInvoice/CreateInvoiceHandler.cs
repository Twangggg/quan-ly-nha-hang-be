using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FoodHub.Application.Features.Invoices.Commands.CreateInvoice
{
    public class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, Result<CreateInvoiceResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateInvoiceHandler> _logger;

        public CreateInvoiceHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<CreateInvoiceHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }
        public async Task<Result<CreateInvoiceResponse>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
        {
            var orderRepo = _unitOfWork.Repository<Order>();
            var invoiceRepo = _unitOfWork.Repository<Invoice>();

            // Create a new table entity and populate its properties
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            // Check if an invoice already exists for the given order
            if (invoiceRepo.Query().Any(i => i.OrderId == request.OrderId))
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Invoice.AlreadyExists);
                return Result<CreateInvoiceResponse>.Failure(errorMessage);
            }

            // Check if the order exists
            var order = await orderRepo
                .Query()
                .Include(o => o.OrderItems) // Include related order items if needed
                .ThenInclude(oi => oi.OptionGroups) // Include related menu item if needed
                .ThenInclude(og => og.OptionValues) // Include related menu item if needed
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
            if (order == null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound);
                return Result<CreateInvoiceResponse>.NotFound(errorMessage);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var invoiceId = Guid.NewGuid();
                var orderId = order.OrderId;
                var invoiceNumber = GenerateInvoiceNumber();
                var subTotal = order.TotalAmount; // Assuming TotalAmount is the sum of all order items
                var amountReceived = request.AmountReceived;
                var taxAmount = subTotal * 0.1m; // Assuming a 10% tax rate, adjust as needed
                var amountReturned = amountReceived - (subTotal + taxAmount);

                if (amountReturned < 0)
                {
                    var errorMessage = _messageService.GetMessage(MessageKeys.Invoice.InsufficientAmount);
                    return Result<CreateInvoiceResponse>.Failure(errorMessage);
                }

                var InvoiceItems = order.OrderItems.Select(oi => new InvoiceItem
                {
                    InvoiceItemId = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    ItemName = oi.ItemNameSnapshot ?? "Unknown",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPriceSnapshot,
                    TotalPrice = oi.GetTotalPrice(),
                    Note = oi.ItemNote ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = auditorId
                }).ToList();

                var invoice = new Invoice
                {
                    InvoiceId = invoiceId,
                    OrderId = orderId,
                    InvoiceNumber = invoiceNumber,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount, // Assuming a 10% tax rate, adjust as needed
                    AmountReceived = amountReceived,
                    AmountReturned = amountReturned,
                    CreatedBy = auditorId,
                    CreatedAt = DateTime.UtcNow
                };

                await invoiceRepo.AddAsync(invoice);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                var response = new CreateInvoiceResponse
                {
                    // Map necessary properties from the order to the response
                };

                return Result<CreateInvoiceResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating invoice for order {OrderId}", request.OrderId);

                await _unitOfWork.RollbackTransactionAsync();

                var errorMessage = _messageService.GetMessage(MessageKeys.Invoice.CreateFailed);
                return Result<CreateInvoiceResponse>.Failure(errorMessage);
            }
        }

        public string GenerateInvoiceNumber()
        {
            // Implement your logic to generate a unique invoice number
            // For example, you can use a combination of date and a random number
            return $"INV-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}


