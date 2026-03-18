using FluentValidation;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using MediatR;

namespace FoodHub.Application.Features.Billing.Queries.ExportPreCheckBillPdf
{
    /// <summary>
    /// Yêu cầu xuất file PDF phiếu tạm tính cho một đơn hàng.
    /// </summary>
    public class ExportPreCheckBillPdfQuery : IRequest<Result<ExportPreCheckBillPdfResponse>>
    {
        /// <summary>
        /// ID của đơn hàng cần xuất phiếu tạm tính.
        /// </summary>
        public Guid OrderId { get; set; }
    }

    public class ExportPreCheckBillPdfQueryValidator : AbstractValidator<ExportPreCheckBillPdfQuery>
    {
        public ExportPreCheckBillPdfQueryValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}
