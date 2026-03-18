using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Invoices.Queries.GetInvoicePdf
{
    public class GetInvoicePdfHandler : IRequestHandler<GetInvoicePdfQuery, Result<byte[]>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPdfService _pdfService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetInvoicePdfHandler> _logger;

        public GetInvoicePdfHandler(
            IUnitOfWork unitOfWork,
            IPdfService pdfService,
            IMessageService messageService,
            ILogger<GetInvoicePdfHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _pdfService = pdfService;
            _messageService = messageService;
            _logger = logger;
        }
        public async Task<Result<byte[]>> Handle(GetInvoicePdfQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetInvoicePdfQuery for InvoiceId: {InvoiceId}", request.InvoiceId);
            var invoiceRepo = _unitOfWork.Repository<Invoice>();

            var invoice = await invoiceRepo
                .Query()
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice with ID {InvoiceId} not found", request.InvoiceId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Invoice.NotFound);
                return Result<byte[]>.NotFound(errorMessage);
            }

            _logger.LogInformation("Generating PDF for InvoiceId: {InvoiceId}", request.InvoiceId);
            var pdfBytes = _pdfService.GenerateInvoicePdf(invoice);

            return Result<byte[]>.Success(pdfBytes);
        }
    }
}
