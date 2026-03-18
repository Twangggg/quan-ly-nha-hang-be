using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Queries.ExportPreCheckBillPdf
{
    public class ExportPreCheckBillPdfHandler : IRequestHandler<ExportPreCheckBillPdfQuery, Result<ExportPreCheckBillPdfResponse>>
    {
        private readonly IMediator _mediator;
        private readonly IPdfService _pdfService;
        private readonly ILogger<ExportPreCheckBillPdfHandler> _logger;

        public ExportPreCheckBillPdfHandler(
            IMediator mediator,
            IPdfService pdfService,
            ILogger<ExportPreCheckBillPdfHandler> logger)
        {
            _mediator = mediator;
            _pdfService = pdfService;
            _logger = logger;
        }

        public async Task<Result<ExportPreCheckBillPdfResponse>> Handle(ExportPreCheckBillPdfQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Exporting pre-check bill PDF for OrderId: {OrderId}", request.OrderId);

            var query = new GetPreCheckBillQuery { OrderId = request.OrderId };
            var dataResult = await _mediator.Send(query, cancellationToken);

            if (!dataResult.IsSuccess)
            {
                _logger.LogWarning("Failed to fetch pre-check bill data for PDF export. OrderId: {OrderId}", request.OrderId);
                return Result<ExportPreCheckBillPdfResponse>.Failure(dataResult.Error ?? string.Empty, dataResult.ErrorType);
            }

            var data = dataResult.Data!;
            var pdfBytes = _pdfService.GeneratePreCheckBill(data);

            // Generate descriptive filename: TamTinh_Ban[TableNumber]_[OrderCode]_[yyyyMMdd]_[HHmm].pdf
            var tablePart = data.TableNumber.HasValue ? $"Ban{data.TableNumber.Value:D2}" : "MangVe";
            var orderCode = data.OrderCode;
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            var fileName = $"TamTinh_{tablePart}_{orderCode}_{timestamp}.pdf";

            _logger.LogInformation(
                "Successfully exported pre-check bill PDF for OrderId: {OrderId} with FileName: {FileName}",
                request.OrderId,
                fileName
            );

            return Result<ExportPreCheckBillPdfResponse>.Success(new ExportPreCheckBillPdfResponse
            {
                Content = pdfBytes,
                FileName = fileName
            });
        }
    }
}
