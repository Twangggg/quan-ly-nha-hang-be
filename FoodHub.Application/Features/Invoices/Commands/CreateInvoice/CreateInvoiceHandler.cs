using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Invoices.Commands.CreateInvoice
{
    /// <summary>
    /// Xử lý lệnh tạo hóa đơn (invoice) cho một đơn hàng đã hoàn thành. Handler này sẽ kiểm tra tính hợp lệ của yêu cầu, bao gồm việc xác nhận rằng đơn hàng tồn tại, chưa có hóa đơn nào được tạo cho đơn hàng đó, và nhân viên thu ngân (auditor) tồn tại. Nếu tất cả các điều kiện
    /// </summary>
    public class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, Result<CreateInvoiceResponse>>
    {
        // Khai báo các dịch vụ và repository cần thiết để xử lý lệnh tạo hóa đơn
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
            _logger.LogInformation("Handling CreateInvoiceCommand for OrderId: {OrderId}", request.OrderId);
            // Retrieve the necessary repositories for orders, invoices, and employees
            var orderRepo = _unitOfWork.Repository<Order>();
            var invoiceRepo = _unitOfWork.Repository<Invoice>();
            var employeeRepo = _unitOfWork.Repository<Employee>();
            var tableRepo = _unitOfWork.Repository<Table>();

            // Create a new table entity and populate its properties
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                _logger.LogInformation("Current user ID parsed successfully: {UserId}", parsedId);
                auditorId = parsedId;
            }

            // Check if an invoice already exists for the given order
            if (invoiceRepo.Query().Any(i => i.OrderId == request.OrderId))
            {
                _logger.LogWarning("An invoice already exists for OrderId: {OrderId}", request.OrderId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Invoice.AlreadyExists);
                return Result<CreateInvoiceResponse>.Failure(errorMessage);
            }

            // Check if the employee (auditor) exists
            var employee = await employeeRepo
                .Query()
                .FirstOrDefaultAsync(e => e.EmployeeId == auditorId, cancellationToken);
            if (employee == null)
            {
                _logger.LogWarning("Employee (auditor) not found for UserId: {UserId}", _currentUserService.UserId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Employee.NotFound);
                return Result<CreateInvoiceResponse>.NotFound(errorMessage);
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
                _logger.LogWarning("Order not found for OrderId: {OrderId}", request.OrderId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Order.NotFound);
                return Result<CreateInvoiceResponse>.NotFound(errorMessage);
            }

            var tableId = order.TableId;
            var table = await tableRepo
                .Query()
                .Include(t => t.Area) // Include related area if needed
                .FirstOrDefaultAsync(t => t.TableId == tableId, cancellationToken);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Creating invoice for OrderId: {OrderId}", request.OrderId);
                var invoiceId = Guid.NewGuid();
                var orderId = order.OrderId;
                var invoiceNumber = GenerateInvoiceNumber();
                var subTotal = order.TotalAmount; // Assuming TotalAmount is the sum of all order items
                var amountReceived = request.AmountReceived;
                var taxAmount = subTotal * 0.1m; // Assuming a 10% tax rate, adjust as needed
                var discountAmount = 0m; // Assuming DiscountAmount is the total discount applied to the order
                var totalAmount = subTotal + taxAmount - discountAmount;
                var amountReturned = amountReceived - totalAmount;
                var paymentMethod = order.PaymentMethod;
                var cashierName = employee.FullName;
                var tableNumber = table?.GetTableName() ?? string.Empty;

                _logger.LogInformation("Calculated invoice amounts - SubTotal: {SubTotal}, TaxAmount: {TaxAmount}, DiscountAmount: {DiscountAmount}, TotalAmount: {TotalAmount}, AmountReceived: {AmountReceived}, AmountReturned: {AmountReturned}",
                    subTotal, taxAmount, discountAmount, totalAmount, amountReceived, amountReturned);
                // Validate that the amount received is sufficient to cover the total amount
                if (amountReturned < 0)
                {
                    _logger.LogWarning("Insufficient amount received for OrderId: {OrderId}. AmountReceived: {AmountReceived}, TotalAmount: {TotalAmount}", request.OrderId, amountReceived, totalAmount);
                    var errorMessage = _messageService.GetMessage(MessageKeys.Invoice.InsufficientAmount);
                    return Result<CreateInvoiceResponse>.Failure(errorMessage);
                }

                if (!order.OrderItems.Any())
                {
                    _logger.LogWarning("Order with OrderId: {OrderId} has no order items. Cannot create invoice.", request.OrderId);
                    var errorMessage = _messageService.GetMessage(MessageKeys.Order.EmptyOrder);
                    return Result<CreateInvoiceResponse>.Failure(errorMessage);
                }

                _logger.LogInformation("Creating invoice items for OrderId: {OrderId}", request.OrderId);
                // Create invoice items based on order items
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

                _logger.LogInformation("Creating invoice entity for OrderId: {OrderId}", request.OrderId);
                var invoice = new Invoice
                {
                    // Set invoice properties based on order details and request data
                    InvoiceId = invoiceId,
                    OrderId = orderId,
                    InvoiceNumber = invoiceNumber,

                    // Calculate amounts based on order details
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    AmountReceived = amountReceived,
                    AmountReturned = amountReturned,
                    PaymentMethod = paymentMethod ?? PaymentMethod.Cash,

                    // Set audit fields
                    CashierName = cashierName,
                    TableNumber = tableNumber,
                    CreatedBy = auditorId,
                    CreatedAt = DateTime.UtcNow,


                    // Set related invoice items
                    Items = InvoiceItems
                };

                _logger.LogInformation("Adding invoice to repository for OrderId: {OrderId}", request.OrderId);
                await invoiceRepo.AddAsync(invoice);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Invoice created successfully for OrderId: {OrderId} with InvoiceId: {InvoiceId}", request.OrderId, invoiceId);

                var response = new CreateInvoiceResponse
                {
                    InvoiceId = invoiceId,
                    InvoiceNumber = invoiceNumber
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


