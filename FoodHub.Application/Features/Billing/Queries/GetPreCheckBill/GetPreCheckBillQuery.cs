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

namespace FoodHub.Application.Features.Billing.Queries.GetPreCheckBill
{
    public record GetPreCheckBillQuery : IRequest<Result<GetPreCheckBillResponse>>
    {
        /// <summary>
        /// ID của đơn hàng cần xem phiếu tạm tính.
        /// </summary>
        public Guid OrderId { get; set; }
    }

    public class GetPreCheckBillQueryValidator : AbstractValidator<GetPreCheckBillQuery>
    {
        public GetPreCheckBillQueryValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}
