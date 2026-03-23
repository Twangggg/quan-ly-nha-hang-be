using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Invoices.Queries.GetInvoiceById
{
    public class GetInvoiceByIdHandler : IRequestHandler<GetInvoiceByIdQuery, Result<GetInvoiceByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetInvoiceByIdHandler> _logger;

        public GetInvoiceByIdHandler(IUnitOfWork unitOfWork, ILogger<GetInvoiceByIdHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<GetInvoiceByIdResponse>> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetInvoiceByIdQuery for InvoiceId: {InvoiceId}", request.InvoiceId);
            var invoiceRepo = _unitOfWork.Repository<Invoice>();

            var invoice = await invoiceRepo
                .Query()
                .Include(inv => inv.Items)
                .FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice with Id: {InvoiceId} not found", request.InvoiceId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Invoice.NotFound);
                return Result<GetInvoiceByIdResponse>.Failure(errorMessage);
            }

            _logger.LogInformation("Invoice with Id: {InvoiceId} found, preparing response", request.InvoiceId);
            var response = new GetInvoiceByIdResponse
            {
                InvoiceId = request.InvoiceId,
                OrderId = invoice.OrderId,
                InvoiceNumber = invoice.InvoiceNumber,

                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount,
                DiscountAmount = invoice.DiscountAmount,

                TotalAmount = invoice.TotalAmount,
                AmountReceived = invoice.AmountReceived,
                AmountReturned = invoice.AmountReturned,

                PaymentMethod = invoice.PaymentMethod,
                PaymentMethodCode = invoice.PaymentMethod.ToString(),

                CashierName = invoice.CashierName,
                TableNumber = invoice.TableNumber ?? string.Empty,

                Items = invoice.Items.Select(item => new InvoiceItemResponse
                {
                    InvoiceItemId = item.InvoiceItemId,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Note = item.Note
                }).ToList()
            };

            return Result<GetInvoiceByIdResponse>.Success(response);
        }
    }
}
